using GithubAnalyzer.WebApi.Database;
using Microsoft.EntityFrameworkCore;

namespace GithubAnalyzer.WebApi.Extensions;

public static class PersistenceExtensions
{
    public const string PostgresDbConnectionName = "postgresdb";

    public static IHostApplicationBuilder AddApplicationPersistence(
        this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AppDbContext>(PostgresDbConnectionName,
            settings => settings.DisableTracing = builder.Environment.IsEnvironment("Testing"));

        return builder;
    }

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            // Apply pending migrations in development mode
            await dbContext.Database.MigrateAsync();
        }
    }
}
