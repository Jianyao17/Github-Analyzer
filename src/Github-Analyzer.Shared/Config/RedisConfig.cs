namespace GithubAnalyzer.Shared.Config;

/// <summary>
/// Opsi konfigurasi untuk Redis Streams consumer group dan penanganan job analisis.
/// </summary>
public sealed class RedisConfig
{
    /// <summary>
    /// Nama section konfigurasi pada appsettings.json.
    /// </summary>
    public const string SectionName = "RedisBroker";

    /// <summary>
    /// Nama stream Redis tempat job analisis dikirimkan oleh Web API.
    /// Default: "analysis:jobs".
    /// </summary>
    public string StreamName { get; set; } = "analysis:jobs";

    /// <summary>
    /// Nama consumer group untuk worker yang memproses analisis.
    /// Default: "analysis-workers".
    /// </summary>
    public string ConsumerGroup { get; set; } = "analysis-workers";

    /// <summary>
    /// Prefix channel Redis Pub/Sub untuk stream progress analisis real-time.
    /// Default: "analysis:progress".
    /// </summary>
    public string ProgressChannelPrefix { get; set; } = "analysis:progress";

    /// <summary>
    /// Jumlah maksimum percobaan pengiriman sebelum job dianggap dead-letter (poison pill) dan di-ACK.
    /// Default: 3 kali.
    /// </summary>
    public int MaxDeliveryAttempts { get; set; } = 3;

    /// <summary>
    /// Jeda waktu tunggu antar polling ketika tidak ada pesan baru di dalam stream.
    /// Default: 1 detik.
    /// </summary>
    public TimeSpan PollDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Ambang batas waktu idle sebelum job yang belum di-ACK dianggap terlantar/gagal dan di-claim ulang.
    /// Default: 5 menit.
    /// </summary>
    public TimeSpan StaleIdleTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Interval pengecekan berkala untuk auto-claim job yang stale di Pending Entries List (PEL).
    /// Default: 30 detik.
    /// </summary>
    public TimeSpan StaleCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
}
