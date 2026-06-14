using GithubAnalyzer.WebApi.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;

namespace GithubAnalyzer.WebApi.Extensions;

public static class OutputCacheExtensions
{
    public const string UserCachePolicyName = "UserCache";

    /// <summary>
    /// Mendaftarkan Output Cache. Jika Redis dikonfigurasi, cache menggunakan Redis
    /// dengan <see cref="ResiliencyOutputCacheStore"/> sebagai wrapper yang menjamin
    /// Redis error tidak mengganggu operasi DB maupun request pipeline.
    /// Jika Redis tidak dikonfigurasi, output cache di-disable (NoOp) sepenuhnya.
    /// </summary>
    public static IHostApplicationBuilder AddProjectOutputCache(
        this IHostApplicationBuilder builder)
    {
        var redisConnectionString = builder.Configuration.GetConnectionString("cache");
        var useRedis = !string.IsNullOrWhiteSpace(redisConnectionString);

        if (useRedis)
        {
            // Baca settings timeout dari appsettings (dengan default jika tidak dikonfigurasi)
            var settings = builder.Configuration
                .GetSection(OutputCacheSettings.SectionName)
                .Get<OutputCacheSettings>() ?? new OutputCacheSettings();

            // Konfigurasi koneksi Redis dengan timeout yang jelas agar tidak hanging.
            // AbortOnConnectFail=false memastikan aplikasi tetap berjalan meski Redis tidak tersedia.
            builder.AddRedisOutputCache("cache", configureOptions: options =>
            {
                // options adalah ConfigurationOptions dari StackExchange.Redis
                options.ConnectTimeout     = settings.ConnectTimeoutMs;
                options.SyncTimeout        = settings.SyncTimeoutMs;
                options.AbortOnConnectFail = false;
            });

            // AddRedisOutputCache mendaftarkan IOutputCacheStore sebagai singleton.
            // Kita "wrap" dengan ResiliencyOutputCacheStore menggunakan teknik replace descriptor:
            // 1. Cari descriptor IOutputCacheStore yang baru saja didaftarkan.
            // 2. Buat descriptor baru sebagai factory yang membungkus factory lama.
            // 3. Ganti descriptor lama dengan yang baru.
            WrapOutputCacheStoreWithResiliency(builder.Services);
        }
        else
        {
            // Redis tidak dikonfigurasi → daftarkan NoOpOutputCacheStore agar DI tidak crash
            // di endpoint yang meng-inject IOutputCacheStore untuk eviction.
            builder.Services.AddSingleton<IOutputCacheStore, NoOpOutputCacheStore>();
        }

        // AddOutputCache SELALU dipanggil (baik Redis ada maupun tidak) agar:
        // 1. Policy "UserCache" selalu terdaftar → endpoint RequireUserCache() tidak crash.
        // 2. Middleware UseOutputCache() bisa dipanggil tanpa DI error.
        // Ketika Redis tidak ada, IOutputCacheStore di-resolve ke NoOpOutputCacheStore
        // sehingga middleware tidak melakukan caching apapun secara transparan.
        builder.Services.AddOutputCache(options =>
        {
            options.AddPolicy(UserCachePolicyName, new UserSpecificCachePolicy());
        });

        return builder;
    }

    /// <summary>
    /// Mengaktifkan middleware Output Cache di pipeline. Middleware hanya aktif
    /// jika store yang terdaftar bukan <see cref="NoOpOutputCacheStore"/>.
    /// Pengecekan menggunakan DI lebih reliable daripada membaca ulang config string.
    /// </summary>
    public static IApplicationBuilder UseProjectCache(this IApplicationBuilder app)
    {
        var store = app.ApplicationServices.GetRequiredService<IOutputCacheStore>();
        if (store is NoOpOutputCacheStore)
            return app;

        app.UseOutputCache();
        return app;
    }

    /// <summary>
    /// Menerapkan policy <see cref="UserCachePolicyName"/> pada endpoint.
    /// </summary>
    public static TBuilder RequireUserCache<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.CacheOutput(UserCachePolicyName);
    }

    /// <summary>
    /// Melakukan eviction cache berdasarkan tag secara best-effort.
    /// Logging ditangani secara internal oleh <see cref="ResiliencyOutputCacheStore"/>,
    /// sehingga caller tidak perlu menyediakan logger.
    /// </summary>
    public static async ValueTask TryEvictByTagAsync(
        this IOutputCacheStore store, string tag,
        CancellationToken ct = default)
    {
        try
        {
            await store.EvictByTagAsync(tag, ct);
        }
        catch
        {
            // Tangkap exception sebagai last-resort safety net.
            // Dalam kondisi normal, ResiliencyOutputCacheStore sudah menangkap dan
            // me-log semua Redis exception sebelum sampai ke sini.
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Internal helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mencari descriptor <see cref="IOutputCacheStore"/> yang didaftarkan oleh
    /// <c>AddRedisOutputCache</c> dan menggantinya dengan descriptor baru yang
    /// membungkus store asli dengan <see cref="ResiliencyOutputCacheStore"/>.
    /// </summary>
    private static void WrapOutputCacheStoreWithResiliency(IServiceCollection services)
    {
        // Cari descriptor terakhir IOutputCacheStore (yang didaftarkan AddRedisOutputCache)
        var descriptor = services.LastOrDefault(
            d => d.ServiceType == typeof(IOutputCacheStore));

        if (descriptor is null)
            return;

        // Buat factory yang membungkus inner store dengan ResiliencyOutputCacheStore
        services.Replace(ServiceDescriptor.Singleton<IOutputCacheStore>(sp =>
        {
            // Buat instance inner store dari descriptor asli
            IOutputCacheStore inner = descriptor switch
            {
                { ImplementationInstance: not null } => (IOutputCacheStore)descriptor.ImplementationInstance,
                { ImplementationFactory: not null }  => (IOutputCacheStore)descriptor.ImplementationFactory(sp),
                { ImplementationType: not null }     => (IOutputCacheStore)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType),
                _                                    => new NoOpOutputCacheStore()
            };

            var logger = sp.GetRequiredService<ILogger<ResiliencyOutputCacheStore>>();
            return new ResiliencyOutputCacheStore(inner, logger);
        }));
    }
}

// ────────────────────────────────────────────────────────────────────────────
// No-Op Store — digunakan ketika Redis tidak dikonfigurasi
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Implementasi <see cref="IOutputCacheStore"/> yang tidak melakukan apapun.
/// Digunakan sebagai placeholder ketika Redis tidak dikonfigurasi sehingga
/// DI tetap dapat me-resolve <see cref="IOutputCacheStore"/> tanpa error.
/// </summary>
public sealed class NoOpOutputCacheStore : IOutputCacheStore
{
    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
        => ValueTask.FromResult<byte[]?>(null);

    public ValueTask SetAsync(
        string key, byte[] value, string[]? tags,
        TimeSpan validFor, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

// ────────────────────────────────────────────────────────────────────────────
// Resilient Store — decorator di atas Redis store
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Decorator <see cref="IOutputCacheStore"/> yang membungkus Redis store dengan
/// penanganan error. Fitur utama:
/// <list type="bullet">
///   <item>Menangkap semua Redis exception tanpa melemparnya ke caller.</item>
///   <item>
///     Otomatis men-disable dirinya sendiri setelah error pertama — tidak
///     fallback ke in-memory. Cache di-bypass sepenuhnya hingga aplikasi di-restart.
///   </item>
///   <item>
///     Thread-safe: menggunakan <see cref="Interlocked.CompareExchange"/>
///     untuk flag auto-disable.
///   </item>
/// </list>
/// </summary>
public sealed class ResiliencyOutputCacheStore(
    IOutputCacheStore inner, ILogger<ResiliencyOutputCacheStore> logger) : IOutputCacheStore
{
    // 0 = enabled, 1 = disabled
    private int _disabled;

    private bool IsDisabled => Volatile.Read(ref _disabled) == 1;

    /// <summary>
    /// Menonaktifkan store dan mencatat log warning. Hanya dieksekusi sekali
    /// menggunakan <see cref="Interlocked.CompareExchange"/> agar thread-safe.
    /// </summary>
    private void DisableAndWarn(Exception ex)
    {
        if (Interlocked.CompareExchange(ref _disabled, 1, 0) == 0)
        {
            logger.LogWarning(ex,
                "Output cache Redis store mengalami error. Cache di-disable untuk sesi ini. " +
                "Restart aplikasi untuk mengaktifkan kembali.");
        }
    }

    public async ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        if (IsDisabled) return;
        try
        {
            await inner.EvictByTagAsync(tag, cancellationToken);
        }
        catch (Exception ex)
        {
            DisableAndWarn(ex);
        }
    }

    public async ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        if (IsDisabled) return null;
        try
        {
            return await inner.GetAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            DisableAndWarn(ex);
            return null;
        }
    }

    public async ValueTask SetAsync(
        string key, byte[] value, string[]? tags,
        TimeSpan validFor, CancellationToken cancellationToken)
    {
        if (IsDisabled) return;
        try
        {
            await inner.SetAsync(key, value, tags, validFor, cancellationToken);
        }
        catch (Exception ex)
        {
            DisableAndWarn(ex);
        }
    }
}
