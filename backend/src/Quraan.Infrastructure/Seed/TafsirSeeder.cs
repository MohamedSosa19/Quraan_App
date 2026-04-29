using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Seed;

public sealed class TafsirSeeder
{
    private readonly QuraanDbContext _db;
    private readonly string _sourcesDir;
    private readonly ILogger<TafsirSeeder> _log;

    public TafsirSeeder(QuraanDbContext db, string sourcesDir, ILogger<TafsirSeeder> log)
    {
        _db = db;
        _sourcesDir = sourcesDir;
        _log = log;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var path = Path.Combine(_sourcesDir, "en-tafisr-ibn-kathir.json");
        if (!File.Exists(path))
        {
            _log.LogWarning("Tafsir source missing; skipping. See Seed/Sources/README.md.");
            return;
        }
        if (await _db.TafsirSources.AnyAsync(ct).ConfigureAwait(false))
        {
            _log.LogInformation("Tafsir already seeded; skipping.");
            return;
        }

        var source = new TafsirSource
        {
            Id = 1,
            Code = "ibn-kathir-en",
            Name = "Tafsir Ibn Kathir (Mubarakpuri abridged)",
            Language = "en",
            Attribution = "Public-domain English digest by Mawlana Safi-ur-Rahman Mubarakpuri (spa5k/tafsir_api)."
        };
        _db.TafsirSources.Add(source);

        await using var stream = File.OpenRead(path);
        var entries = await JsonSerializer.DeserializeAsync<List<TafsirRow>>(stream, _options, ct)
            .ConfigureAwait(false) ?? new();

        // Map (surah, ayah) → AyahId from already-seeded Ayahs.
        var ayahIdByPosition = await _db.Ayahs.AsNoTracking()
            .Select(a => new { a.SurahId, a.NumberInSurah, a.Id })
            .ToDictionaryAsync(x => (x.SurahId, x.NumberInSurah), x => x.Id, ct)
            .ConfigureAwait(false);

        int written = 0;
        foreach (var row in entries)
        {
            if (!ayahIdByPosition.TryGetValue(((byte)row.Sura, (short)row.Aya), out var ayahId))
                continue;
            _db.TafsirEntries.Add(new TafsirEntry
            {
                TafsirSourceId = source.Id,
                AyahId = ayahId,
                Body = row.Text ?? string.Empty
            });
            written++;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _log.LogInformation("Seeded {Count} Tafsir entries.", written);
    }

    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private sealed class TafsirRow
    {
        [JsonPropertyName("sura")] public int Sura { get; set; }
        [JsonPropertyName("aya")] public int Aya { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
    }
}
