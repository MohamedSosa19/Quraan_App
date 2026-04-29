using Microsoft.EntityFrameworkCore;
using Quraan.Application.Search;
using Quraan.Domain.Common;
using Quraan.Domain.Entities;
using Quraan.Infrastructure.Persistence;

namespace Quraan.IntegrationTests.Fixtures;

/// <summary>
/// Seeds a deterministic minimal content set sufficient for US1 integration
/// tests (114 Surah headers + Al-Fatiha's 7 Ayahs + en.sahih translation).
/// Real-content seeding (Tanzil + Saheeh + Ibn Kathir) is the job of
/// <c>QuranSeeder</c>; this fixture seeder runs in test isolation.
/// </summary>
public static class TestDataSeeder
{
    public static async Task SeedAsync(QuraanDbContext db)
    {
        if (await db.Surahs.AnyAsync().ConfigureAwait(false)) return;

        db.Translations.Add(new Translation
        {
            Id = 1,
            Code = "en.sahih",
            Language = "en",
            Name = "Saheeh International",
            Attribution = "Saheeh International (used with permission for non-commercial purposes)",
        });

        var surahs = BuildAll114Surahs();
        await db.Surahs.AddRangeAsync(surahs).ConfigureAwait(false);

        var ayahs = BuildAlFatihaAyahs();
        await db.Ayahs.AddRangeAsync(ayahs).ConfigureAwait(false);

        var translations = BuildAlFatihaTranslations();
        await db.AyahTranslations.AddRangeAsync(translations).ConfigureAwait(false);

        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    private static IEnumerable<Surah> BuildAll114Surahs()
    {
        var meta = SurahMetadata.All;
        for (byte id = 1; id <= 114; id++)
        {
            var (arabic, transliterated, english, place, ayahCount) = meta[id - 1];
            yield return new Surah
            {
                Id = id,
                ArabicName = arabic,
                TransliteratedName = transliterated,
                EnglishName = english,
                EnglishNameNormalized = english.ToLowerInvariant(),
                RevelationPlace = place,
                AyahCount = ayahCount,
                OrderInRevelation = id,
            };
        }
    }

    private static readonly (string ArabicText, string EnglishText)[] s_alFatiha =
    [
        ("بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ", "In the name of Allah, the Entirely Merciful, the Especially Merciful."),
        ("الْحَمْدُ لِلَّهِ رَبِّ الْعَالَمِينَ", "[All] praise is [due] to Allah, Lord of the worlds."),
        ("الرَّحْمَٰنِ الرَّحِيمِ", "The Entirely Merciful, the Especially Merciful."),
        ("مَالِكِ يَوْمِ الدِّينِ", "Sovereign of the Day of Recompense."),
        ("إِيَّاكَ نَعْبُدُ وَإِيَّاكَ نَسْتَعِينُ", "It is You we worship and You we ask for help."),
        ("اهْدِنَا الصِّرَاطَ الْمُسْتَقِيمَ", "Guide us to the straight path."),
        ("صِرَاطَ الَّذِينَ أَنْعَمْتَ عَلَيْهِمْ غَيْرِ الْمَغْضُوبِ عَلَيْهِمْ وَلَا الضَّالِّينَ", "The path of those upon whom You have bestowed favor, not of those who have evoked [Your] anger or of those who are astray."),
    ];

    private static IEnumerable<Ayah> BuildAlFatihaAyahs()
    {
        for (short n = 1; n <= 7; n++)
        {
            var (arabic, _) = s_alFatiha[n - 1];
            yield return new Ayah
            {
                Id = n,
                SurahId = 1,
                NumberInSurah = n,
                ArabicText = arabic,
                NormalizedArabicText = ArabicNormalizer.ForIngest(arabic),
                JuzNumber = 1,
                HizbQuarter = 1,
                Sajda = false,
            };
        }
    }

    private static IEnumerable<AyahTranslation> BuildAlFatihaTranslations()
    {
        for (short n = 1; n <= 7; n++)
        {
            var (_, english) = s_alFatiha[n - 1];
            yield return new AyahTranslation
            {
                AyahId = n,
                TranslationId = 1,
                Text = english,
                NormalizedText = english.ToLowerInvariant(),
            };
        }
    }

    private static class SurahMetadata
    {
        public static readonly (string Arabic, string Transliterated, string English, RevelationPlace Place, short AyahCount)[] All =
        [
            ("الفاتحة","Al-Fatihah","The Opener",RevelationPlace.Meccan,7),
            ("البقرة","Al-Baqarah","The Cow",RevelationPlace.Medinan,286),
            ("آل عمران","Aal-E-Imran","The Family of Imran",RevelationPlace.Medinan,200),
            ("النساء","An-Nisa","The Women",RevelationPlace.Medinan,176),
            ("المائدة","Al-Ma'idah","The Table Spread",RevelationPlace.Medinan,120),
            ("الأنعام","Al-An'am","The Cattle",RevelationPlace.Meccan,165),
            ("الأعراف","Al-A'raf","The Heights",RevelationPlace.Meccan,206),
            ("الأنفال","Al-Anfal","The Spoils of War",RevelationPlace.Medinan,75),
            ("التوبة","At-Tawbah","The Repentance",RevelationPlace.Medinan,129),
            ("يونس","Yunus","Jonah",RevelationPlace.Meccan,109),
            ("هود","Hud","Hud",RevelationPlace.Meccan,123),
            ("يوسف","Yusuf","Joseph",RevelationPlace.Meccan,111),
            ("الرعد","Ar-Ra'd","The Thunder",RevelationPlace.Medinan,43),
            ("إبراهيم","Ibrahim","Abraham",RevelationPlace.Meccan,52),
            ("الحجر","Al-Hijr","The Rocky Tract",RevelationPlace.Meccan,99),
            ("النحل","An-Nahl","The Bee",RevelationPlace.Meccan,128),
            ("الإسراء","Al-Isra","The Night Journey",RevelationPlace.Meccan,111),
            ("الكهف","Al-Kahf","The Cave",RevelationPlace.Meccan,110),
            ("مريم","Maryam","Mary",RevelationPlace.Meccan,98),
            ("طه","Ta-Ha","Ta-Ha",RevelationPlace.Meccan,135),
            ("الأنبياء","Al-Anbiya","The Prophets",RevelationPlace.Meccan,112),
            ("الحج","Al-Hajj","The Pilgrimage",RevelationPlace.Medinan,78),
            ("المؤمنون","Al-Mu'minun","The Believers",RevelationPlace.Meccan,118),
            ("النور","An-Nur","The Light",RevelationPlace.Medinan,64),
            ("الفرقان","Al-Furqan","The Criterion",RevelationPlace.Meccan,77),
            ("الشعراء","Ash-Shu'ara","The Poets",RevelationPlace.Meccan,227),
            ("النمل","An-Naml","The Ants",RevelationPlace.Meccan,93),
            ("القصص","Al-Qasas","The Stories",RevelationPlace.Meccan,88),
            ("العنكبوت","Al-'Ankabut","The Spider",RevelationPlace.Meccan,69),
            ("الروم","Ar-Rum","The Romans",RevelationPlace.Meccan,60),
            ("لقمان","Luqman","Luqman",RevelationPlace.Meccan,34),
            ("السجدة","As-Sajdah","The Prostration",RevelationPlace.Meccan,30),
            ("الأحزاب","Al-Ahzab","The Combined Forces",RevelationPlace.Medinan,73),
            ("سبأ","Saba","Sheba",RevelationPlace.Meccan,54),
            ("فاطر","Fatir","Originator",RevelationPlace.Meccan,45),
            ("يس","Ya-Sin","Ya Sin (Yaseen)",RevelationPlace.Meccan,83),
            ("الصافات","As-Saffat","Those who set the Ranks",RevelationPlace.Meccan,182),
            ("ص","Sad","The Letter Saad",RevelationPlace.Meccan,88),
            ("الزمر","Az-Zumar","The Troops",RevelationPlace.Meccan,75),
            ("غافر","Ghafir","The Forgiver",RevelationPlace.Meccan,85),
            ("فصلت","Fussilat","Explained in Detail",RevelationPlace.Meccan,54),
            ("الشورى","Ash-Shuraa","The Consultation",RevelationPlace.Meccan,53),
            ("الزخرف","Az-Zukhruf","The Ornaments of Gold",RevelationPlace.Meccan,89),
            ("الدخان","Ad-Dukhan","The Smoke",RevelationPlace.Meccan,59),
            ("الجاثية","Al-Jathiyah","The Crouching",RevelationPlace.Meccan,37),
            ("الأحقاف","Al-Ahqaf","The Wind-Curved Sandhills",RevelationPlace.Meccan,35),
            ("محمد","Muhammad","Muhammad",RevelationPlace.Medinan,38),
            ("الفتح","Al-Fath","The Victory",RevelationPlace.Medinan,29),
            ("الحجرات","Al-Hujurat","The Rooms",RevelationPlace.Medinan,18),
            ("ق","Qaf","The Letter Qaaf",RevelationPlace.Meccan,45),
            ("الذاريات","Adh-Dhariyat","The Winnowing Winds",RevelationPlace.Meccan,60),
            ("الطور","At-Tur","The Mount",RevelationPlace.Meccan,49),
            ("النجم","An-Najm","The Star",RevelationPlace.Meccan,62),
            ("القمر","Al-Qamar","The Moon",RevelationPlace.Meccan,55),
            ("الرحمن","Ar-Rahman","The Beneficent",RevelationPlace.Medinan,78),
            ("الواقعة","Al-Waqi'ah","The Inevitable",RevelationPlace.Meccan,96),
            ("الحديد","Al-Hadid","The Iron",RevelationPlace.Medinan,29),
            ("المجادلة","Al-Mujadila","The Pleading Woman",RevelationPlace.Medinan,22),
            ("الحشر","Al-Hashr","The Exile",RevelationPlace.Medinan,24),
            ("الممتحنة","Al-Mumtahanah","She that is to be examined",RevelationPlace.Medinan,13),
            ("الصف","As-Saf","The Ranks",RevelationPlace.Medinan,14),
            ("الجمعة","Al-Jumu'ah","The Congregation",RevelationPlace.Medinan,11),
            ("المنافقون","Al-Munafiqun","The Hypocrites",RevelationPlace.Medinan,11),
            ("التغابن","At-Taghabun","The Mutual Disillusion",RevelationPlace.Medinan,18),
            ("الطلاق","At-Talaq","The Divorce",RevelationPlace.Medinan,12),
            ("التحريم","At-Tahrim","The Prohibition",RevelationPlace.Medinan,12),
            ("الملك","Al-Mulk","The Sovereignty",RevelationPlace.Meccan,30),
            ("القلم","Al-Qalam","The Pen",RevelationPlace.Meccan,52),
            ("الحاقة","Al-Haqqah","The Reality",RevelationPlace.Meccan,52),
            ("المعارج","Al-Ma'arij","The Ascending Stairways",RevelationPlace.Meccan,44),
            ("نوح","Nuh","Noah",RevelationPlace.Meccan,28),
            ("الجن","Al-Jinn","The Jinn",RevelationPlace.Meccan,28),
            ("المزمل","Al-Muzzammil","The Enshrouded One",RevelationPlace.Meccan,20),
            ("المدثر","Al-Muddaththir","The Cloaked One",RevelationPlace.Meccan,56),
            ("القيامة","Al-Qiyamah","The Resurrection",RevelationPlace.Meccan,40),
            ("الإنسان","Al-Insan","Man",RevelationPlace.Medinan,31),
            ("المرسلات","Al-Mursalat","The Emissaries",RevelationPlace.Meccan,50),
            ("النبأ","An-Naba","The Tidings",RevelationPlace.Meccan,40),
            ("النازعات","An-Nazi'at","Those who drag forth",RevelationPlace.Meccan,46),
            ("عبس","'Abasa","He frowned",RevelationPlace.Meccan,42),
            ("التكوير","At-Takwir","The Overthrowing",RevelationPlace.Meccan,29),
            ("الانفطار","Al-Infitar","The Cleaving",RevelationPlace.Meccan,19),
            ("المطففين","Al-Mutaffifin","The Defrauding",RevelationPlace.Meccan,36),
            ("الانشقاق","Al-Inshiqaq","The Sundering",RevelationPlace.Meccan,25),
            ("البروج","Al-Buruj","The Mansions of the Stars",RevelationPlace.Meccan,22),
            ("الطارق","At-Tariq","The Morning Star",RevelationPlace.Meccan,17),
            ("الأعلى","Al-A'la","The Most High",RevelationPlace.Meccan,19),
            ("الغاشية","Al-Ghashiyah","The Overwhelming",RevelationPlace.Meccan,26),
            ("الفجر","Al-Fajr","The Dawn",RevelationPlace.Meccan,30),
            ("البلد","Al-Balad","The City",RevelationPlace.Meccan,20),
            ("الشمس","Ash-Shams","The Sun",RevelationPlace.Meccan,15),
            ("الليل","Al-Layl","The Night",RevelationPlace.Meccan,21),
            ("الضحى","Ad-Duha","The Morning Hours",RevelationPlace.Meccan,11),
            ("الشرح","Ash-Sharh","The Relief",RevelationPlace.Meccan,8),
            ("التين","At-Tin","The Fig",RevelationPlace.Meccan,8),
            ("العلق","Al-'Alaq","The Clot",RevelationPlace.Meccan,19),
            ("القدر","Al-Qadr","The Power",RevelationPlace.Meccan,5),
            ("البينة","Al-Bayyinah","The Clear Proof",RevelationPlace.Medinan,8),
            ("الزلزلة","Az-Zalzalah","The Earthquake",RevelationPlace.Medinan,8),
            ("العاديات","Al-'Adiyat","The Courser",RevelationPlace.Meccan,11),
            ("القارعة","Al-Qari'ah","The Calamity",RevelationPlace.Meccan,11),
            ("التكاثر","At-Takathur","The Rivalry in world increase",RevelationPlace.Meccan,8),
            ("العصر","Al-'Asr","The Declining Day",RevelationPlace.Meccan,3),
            ("الهمزة","Al-Humazah","The Traducer",RevelationPlace.Meccan,9),
            ("الفيل","Al-Fil","The Elephant",RevelationPlace.Meccan,5),
            ("قريش","Quraysh","Quraysh",RevelationPlace.Meccan,4),
            ("الماعون","Al-Ma'un","The Small Kindnesses",RevelationPlace.Meccan,7),
            ("الكوثر","Al-Kawthar","The Abundance",RevelationPlace.Meccan,3),
            ("الكافرون","Al-Kafirun","The Disbelievers",RevelationPlace.Meccan,6),
            ("النصر","An-Nasr","The Divine Support",RevelationPlace.Medinan,3),
            ("المسد","Al-Masad","The Palm Fiber",RevelationPlace.Meccan,5),
            ("الإخلاص","Al-Ikhlas","The Sincerity",RevelationPlace.Meccan,4),
            ("الفلق","Al-Falaq","The Daybreak",RevelationPlace.Meccan,5),
            ("الناس","An-Nas","Mankind",RevelationPlace.Meccan,6),
        ];
    }
}
