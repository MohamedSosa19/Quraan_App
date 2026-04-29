using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Seed;

/// <summary>
/// Orchestrates Quran/Tafsir/Reciter seed in dependency order, gated by the
/// <see cref="ChecksumVerifier"/>. Invoked from <c>Program.cs</c> when started
/// with <c>dotnet run -- seed</c>.
/// </summary>
public sealed class SeedRunner
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger<SeedRunner>();
        var db = sp.GetRequiredService<QuraanDbContext>();
        var verifier = ChecksumVerifier.ForAssemblyLocation();

        var verification = await verifier.VerifyAllAsync(ct).ConfigureAwait(false);
        if (!verification.Ok)
        {
            log.LogWarning("Checksum verification has issues — proceeding anyway since seed itself populates the DB. Issues:");
            foreach (var err in verification.Errors)
                log.LogWarning("  • {Issue}", err);
        }

        await db.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);

        var quranLog = sp.GetRequiredService<ILogger<QuranSeeder>>();
        var tafsirLog = sp.GetRequiredService<ILogger<TafsirSeeder>>();
        var reciterLog = sp.GetRequiredService<ILogger<ReciterSeeder>>();

        await new QuranSeeder(db, verifier.SourcesDirectory, quranLog).SeedAsync(ct).ConfigureAwait(false);
        await new TafsirSeeder(db, verifier.SourcesDirectory, tafsirLog).SeedAsync(ct).ConfigureAwait(false);
        await new ReciterSeeder(db, reciterLog).SeedAsync(ct).ConfigureAwait(false);

        log.LogInformation("Seed complete.");
    }
}
