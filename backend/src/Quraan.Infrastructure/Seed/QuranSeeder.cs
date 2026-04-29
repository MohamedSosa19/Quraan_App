using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quraan.Application.Search;
using Quraan.Domain.Common;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.Persistence;

namespace Quraan.Infrastructure.Seed;

public sealed class QuranSeeder
{
    private readonly QuraanDbContext _db;
    private readonly string _sourcesDir;
    private readonly ILogger<QuranSeeder> _log;

    public QuranSeeder(QuraanDbContext db, string sourcesDir, ILogger<QuranSeeder> log)
    {
        _db = db;
        _sourcesDir = sourcesDir;
        _log = log;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var quranPath = Path.Combine(_sourcesDir, "quran-uthmani.txt");
        var translationPath = Path.Combine(_sourcesDir, "en.sahih.txt");
        if (!File.Exists(quranPath) || !File.Exists(translationPath))
        {
            _log.LogWarning("Quran or translation source missing; skipping seed. See Seed/Sources/README.md.");
            return;
        }

        if (await _db.Surahs.AnyAsync(ct).ConfigureAwait(false))
        {
            _log.LogInformation("Surahs already seeded; skipping.");
            return;
        }

        var translation = new Translation
        {
            Id = 1,
            Code = "en.sahih",
            Language = "en",
            Name = "Saheeh International",
            Attribution = "Saheeh International, Almunatada Alislami, public-domain edition (Tanzil)."
        };
        _db.Translations.Add(translation);

        // Parse Tanzil pipe-delimited format: SurahNum|AyahNum|Text
        var arabicLines = await File.ReadAllLinesAsync(quranPath, ct).ConfigureAwait(false);
        var englishLines = await File.ReadAllLinesAsync(translationPath, ct).ConfigureAwait(false);

        var surahs = new Dictionary<byte, Surah>();
        int globalAyahId = 0;
        var ayahsBySurah = new Dictionary<byte, List<Ayah>>();

        foreach (var line in arabicLines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var parts = line.Split('|', 3);
            if (parts.Length != 3) continue;
            var surahId = byte.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            var ayahNum = short.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            var text = parts[2];

            if (!surahs.ContainsKey(surahId))
                surahs[surahId] = SurahMetadata.Create(surahId);

            globalAyahId++;
            var ayah = new Ayah
            {
                Id = globalAyahId,
                SurahId = surahId,
                NumberInSurah = ayahNum,
                ArabicText = text,
                NormalizedArabicText = ArabicNormalizer.ForIngest(text),
                JuzNumber = 1, // placeholder; full juz/hizb mapping requires another source file
                HizbQuarter = 1,
                Sajda = false
            };
            (ayahsBySurah.TryGetValue(surahId, out var list) ? list : ayahsBySurah[surahId] = new List<Ayah>())
                .Add(ayah);
        }

        // English translations — same pipe format, indexed by (surah, ayah).
        var translationsByPos = new Dictionary<(byte, short), string>();
        foreach (var line in englishLines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var parts = line.Split('|', 3);
            if (parts.Length != 3) continue;
            var surahId = byte.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            var ayahNum = short.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            translationsByPos[(surahId, ayahNum)] = parts[2];
        }

        foreach (var surahId in surahs.Keys.OrderBy(k => k))
        {
            var surah = surahs[surahId];
            var ayahs = ayahsBySurah[surahId];
            surah.AyahCount = (short)ayahs.Count;
            _db.Surahs.Add(surah);
            foreach (var ayah in ayahs)
            {
                _db.Ayahs.Add(ayah);
                if (translationsByPos.TryGetValue((surahId, ayah.NumberInSurah), out var enText))
                {
                    _db.AyahTranslations.Add(new AyahTranslation
                    {
                        AyahId = ayah.Id,
                        TranslationId = translation.Id,
                        Text = enText,
                        NormalizedText = enText.ToLowerInvariant()
                    });
                }
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _log.LogInformation("Seeded {SurahCount} Surahs and {AyahCount} Ayahs.", surahs.Count, globalAyahId);
    }

    private static class SurahMetadata
    {
        // Compact 114-row metadata table. Names sourced from the Tanzil index.
        // Format per row: (Id, ArabicName, Translit, English, RevelationPlace).
        // Only fields shown in spec FR-001 are populated; OrderInRevelation is a
        // post-MVP enrichment.
        public static Surah Create(byte id)
        {
            var (ar, tr, en, rev) = _meta[id - 1];
            return new Surah
            {
                Id = id,
                ArabicName = ar,
                TransliteratedName = tr,
                EnglishName = en,
                EnglishNameNormalized = en.ToLowerInvariant(),
                RevelationPlace = rev,
                AyahCount = 0
            };
        }

        // Compact metadata table (id 1..114). Source: Tanzil quran-data.xml.
        private static readonly (string Ar, string Tr, string En, RevelationPlace Rev)[] _meta = new[]
        {
            ("الفاتحة", "Al-Fatiha", "The Opening", RevelationPlace.Meccan),
            ("البقرة", "Al-Baqarah", "The Cow", RevelationPlace.Medinan),
            ("آل عمران", "Aal-E-Imran", "The Family of Imran", RevelationPlace.Medinan),
            ("النساء", "An-Nisa", "The Women", RevelationPlace.Medinan),
            ("المائدة", "Al-Maeda", "The Table", RevelationPlace.Medinan),
            ("الأنعام", "Al-Anam", "The Cattle", RevelationPlace.Meccan),
            ("الأعراف", "Al-Araf", "The Heights", RevelationPlace.Meccan),
            ("الأنفال", "Al-Anfal", "The Spoils of War", RevelationPlace.Medinan),
            ("التوبة", "At-Tawba", "The Repentance", RevelationPlace.Medinan),
            ("يونس", "Yunus", "Jonah", RevelationPlace.Meccan),
            ("هود", "Hud", "Hud", RevelationPlace.Meccan),
            ("يوسف", "Yusuf", "Joseph", RevelationPlace.Meccan),
            ("الرعد", "Ar-Rad", "The Thunder", RevelationPlace.Medinan),
            ("ابراهيم", "Ibrahim", "Abraham", RevelationPlace.Meccan),
            ("الحجر", "Al-Hijr", "The Rocky Tract", RevelationPlace.Meccan),
            ("النحل", "An-Nahl", "The Bee", RevelationPlace.Meccan),
            ("الإسراء", "Al-Isra", "The Night Journey", RevelationPlace.Meccan),
            ("الكهف", "Al-Kahf", "The Cave", RevelationPlace.Meccan),
            ("مريم", "Maryam", "Mary", RevelationPlace.Meccan),
            ("طه", "Taha", "Ta-Ha", RevelationPlace.Meccan),
            ("الأنبياء", "Al-Anbiya", "The Prophets", RevelationPlace.Meccan),
            ("الحج", "Al-Hajj", "The Pilgrimage", RevelationPlace.Medinan),
            ("المؤمنون", "Al-Muminoon", "The Believers", RevelationPlace.Meccan),
            ("النور", "An-Noor", "The Light", RevelationPlace.Medinan),
            ("الفرقان", "Al-Furqan", "The Criterion", RevelationPlace.Meccan),
            ("الشعراء", "Ash-Shuara", "The Poets", RevelationPlace.Meccan),
            ("النمل", "An-Naml", "The Ant", RevelationPlace.Meccan),
            ("القصص", "Al-Qasas", "The Stories", RevelationPlace.Meccan),
            ("العنكبوت", "Al-Ankaboot", "The Spider", RevelationPlace.Meccan),
            ("الروم", "Ar-Room", "The Romans", RevelationPlace.Meccan),
            ("لقمان", "Luqman", "Luqman", RevelationPlace.Meccan),
            ("السجدة", "As-Sajda", "The Prostration", RevelationPlace.Meccan),
            ("الأحزاب", "Al-Ahzab", "The Combined Forces", RevelationPlace.Medinan),
            ("سبإ", "Saba", "Sheba", RevelationPlace.Meccan),
            ("فاطر", "Fatir", "Originator", RevelationPlace.Meccan),
            ("يس", "Ya-Sin", "Ya-Sin", RevelationPlace.Meccan),
            ("الصافات", "As-Saaffat", "Those Who Set the Ranks", RevelationPlace.Meccan),
            ("ص", "Saad", "Saad", RevelationPlace.Meccan),
            ("الزمر", "Az-Zumar", "The Groups", RevelationPlace.Meccan),
            ("غافر", "Ghafir", "The Forgiver", RevelationPlace.Meccan),
            ("فصلت", "Fussilat", "Explained in Detail", RevelationPlace.Meccan),
            ("الشورى", "Ash-Shura", "Consultation", RevelationPlace.Meccan),
            ("الزخرف", "Az-Zukhruf", "Ornaments of Gold", RevelationPlace.Meccan),
            ("الدخان", "Ad-Dukhan", "The Smoke", RevelationPlace.Meccan),
            ("الجاثية", "Al-Jathiya", "Crouching", RevelationPlace.Meccan),
            ("الأحقاف", "Al-Ahqaf", "The Wind-Curved Sandhills", RevelationPlace.Meccan),
            ("محمد", "Muhammad", "Muhammad", RevelationPlace.Medinan),
            ("الفتح", "Al-Fath", "The Victory", RevelationPlace.Medinan),
            ("الحجرات", "Al-Hujraat", "The Rooms", RevelationPlace.Medinan),
            ("ق", "Qaf", "Qaf", RevelationPlace.Meccan),
            ("الذاريات", "Adh-Dhariyat", "The Winnowing Winds", RevelationPlace.Meccan),
            ("الطور", "At-Tur", "The Mount", RevelationPlace.Meccan),
            ("النجم", "An-Najm", "The Star", RevelationPlace.Meccan),
            ("القمر", "Al-Qamar", "The Moon", RevelationPlace.Meccan),
            ("الرحمن", "Ar-Rahman", "The Beneficent", RevelationPlace.Medinan),
            ("الواقعة", "Al-Waqia", "The Inevitable", RevelationPlace.Meccan),
            ("الحديد", "Al-Hadid", "The Iron", RevelationPlace.Medinan),
            ("المجادلة", "Al-Mujadila", "The Pleading Woman", RevelationPlace.Medinan),
            ("الحشر", "Al-Hashr", "The Exile", RevelationPlace.Medinan),
            ("الممتحنة", "Al-Mumtahana", "She that is to be examined", RevelationPlace.Medinan),
            ("الصف", "As-Saff", "The Ranks", RevelationPlace.Medinan),
            ("الجمعة", "Al-Jumua", "Friday", RevelationPlace.Medinan),
            ("المنافقون", "Al-Munafiqoon", "The Hypocrites", RevelationPlace.Medinan),
            ("التغابن", "At-Taghabun", "Mutual Disillusion", RevelationPlace.Medinan),
            ("الطلاق", "At-Talaq", "Divorce", RevelationPlace.Medinan),
            ("التحريم", "At-Tahrim", "The Prohibition", RevelationPlace.Medinan),
            ("الملك", "Al-Mulk", "The Sovereignty", RevelationPlace.Meccan),
            ("القلم", "Al-Qalam", "The Pen", RevelationPlace.Meccan),
            ("الحاقة", "Al-Haaqqa", "The Reality", RevelationPlace.Meccan),
            ("المعارج", "Al-Maarij", "The Ascending Stairways", RevelationPlace.Meccan),
            ("نوح", "Nooh", "Noah", RevelationPlace.Meccan),
            ("الجن", "Al-Jinn", "The Jinn", RevelationPlace.Meccan),
            ("المزمل", "Al-Muzzammil", "The Enshrouded One", RevelationPlace.Meccan),
            ("المدثر", "Al-Muddaththir", "The Cloaked One", RevelationPlace.Meccan),
            ("القيامة", "Al-Qiyama", "The Resurrection", RevelationPlace.Meccan),
            ("الانسان", "Al-Insan", "Man", RevelationPlace.Medinan),
            ("المرسلات", "Al-Mursalat", "The Emissaries", RevelationPlace.Meccan),
            ("النبإ", "An-Naba", "The Tidings", RevelationPlace.Meccan),
            ("النازعات", "An-Naziat", "Those Who Drag Forth", RevelationPlace.Meccan),
            ("عبس", "Abasa", "He Frowned", RevelationPlace.Meccan),
            ("التكوير", "At-Takwir", "The Overthrowing", RevelationPlace.Meccan),
            ("الإنفطار", "Al-Infitar", "The Cleaving", RevelationPlace.Meccan),
            ("المطففين", "Al-Mutaffifin", "Defrauding", RevelationPlace.Meccan),
            ("الإنشقاق", "Al-Inshiqaq", "The Splitting Open", RevelationPlace.Meccan),
            ("البروج", "Al-Burooj", "The Mansions of the Stars", RevelationPlace.Meccan),
            ("الطارق", "At-Tariq", "The Morning Star", RevelationPlace.Meccan),
            ("الأعلى", "Al-Ala", "The Most High", RevelationPlace.Meccan),
            ("الغاشية", "Al-Ghashiya", "The Overwhelming", RevelationPlace.Meccan),
            ("الفجر", "Al-Fajr", "The Dawn", RevelationPlace.Meccan),
            ("البلد", "Al-Balad", "The City", RevelationPlace.Meccan),
            ("الشمس", "Ash-Shams", "The Sun", RevelationPlace.Meccan),
            ("الليل", "Al-Lail", "The Night", RevelationPlace.Meccan),
            ("الضحى", "Ad-Dhuha", "The Morning Hours", RevelationPlace.Meccan),
            ("الشرح", "Ash-Sharh", "The Relief", RevelationPlace.Meccan),
            ("التين", "At-Tin", "The Fig", RevelationPlace.Meccan),
            ("العلق", "Al-Alaq", "The Clot", RevelationPlace.Meccan),
            ("القدر", "Al-Qadr", "The Power", RevelationPlace.Meccan),
            ("البينة", "Al-Bayyina", "The Clear Proof", RevelationPlace.Medinan),
            ("الزلزلة", "Az-Zalzala", "The Earthquake", RevelationPlace.Medinan),
            ("العاديات", "Al-Adiyat", "The Coursers", RevelationPlace.Meccan),
            ("القارعة", "Al-Qaria", "The Calamity", RevelationPlace.Meccan),
            ("التكاثر", "At-Takathur", "The Rivalry in World Increase", RevelationPlace.Meccan),
            ("العصر", "Al-Asr", "The Declining Day", RevelationPlace.Meccan),
            ("الهمزة", "Al-Humaza", "The Traducer", RevelationPlace.Meccan),
            ("الفيل", "Al-Fil", "The Elephant", RevelationPlace.Meccan),
            ("قريش", "Quraish", "Quraysh", RevelationPlace.Meccan),
            ("الماعون", "Al-Maun", "Almsgiving", RevelationPlace.Meccan),
            ("الكوثر", "Al-Kawthar", "Abundance", RevelationPlace.Meccan),
            ("الكافرون", "Al-Kafiroon", "The Disbelievers", RevelationPlace.Meccan),
            ("النصر", "An-Nasr", "Divine Support", RevelationPlace.Medinan),
            ("المسد", "Al-Masad", "The Palm Fiber", RevelationPlace.Meccan),
            ("الإخلاص", "Al-Ikhlas", "The Sincerity", RevelationPlace.Meccan),
            ("الفلق", "Al-Falaq", "The Daybreak", RevelationPlace.Meccan),
            ("الناس", "An-Nas", "Mankind", RevelationPlace.Meccan),
        };
    }
}
