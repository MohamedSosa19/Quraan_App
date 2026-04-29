using System.Globalization;
using System.Text;

namespace Quraan.Application.Search;

/// <summary>
/// Implements R-05: 7-step normalization pipeline for both ingest-time and
/// query-time matching against <see cref="Quraan.Domain.Entities.Ayah.NormalizedArabicText"/>
/// and <see cref="Quraan.Domain.Entities.AyahTranslation.NormalizedText"/>.
/// </summary>
public static class ArabicNormalizer
{
    /// <summary>Normalizer used at *ingest* time. Steps 1-5 + 7. Tāʾ marbūṭa preserved.</summary>
    public static string ForIngest(string input) => Normalize(input, normalizeTaaMarbuta: false);

    /// <summary>Normalizer used at *query* time. All 7 steps including Tāʾ marbūṭa folding.</summary>
    public static string ForQuery(string input) => Normalize(input, normalizeTaaMarbuta: true);

    private static string Normalize(string? input, bool normalizeTaaMarbuta)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (var rune in input.EnumerateRunes())
        {
            var cp = rune.Value;

            // 1. Strip combining tashkeel marks U+064B..U+065F
            if (cp >= 0x064B && cp <= 0x065F) continue;
            // 2a. Dagger alif U+0670
            if (cp == 0x0670) continue;
            // 2b. Small high marks U+06D6..U+06ED (Quranic annotations)
            if (cp >= 0x06D6 && cp <= 0x06ED) continue;
            // 3. Tatweel U+0640
            if (cp == 0x0640) continue;
            // 4. Alif variants → bare Alif (0627)
            if (cp == 0x0623 || cp == 0x0625 || cp == 0x0622 || cp == 0x0671)
            {
                sb.Append('ا');
                continue;
            }
            // 5. Yāʾ variants: ى (0649) → ي (064A)
            if (cp == 0x0649)
            {
                sb.Append('ي');
                continue;
            }
            // 6. Tāʾ marbūṭa ة (0629) → ه (0647) — query-time only
            if (cp == 0x0629)
            {
                sb.Append(normalizeTaaMarbuta ? 'ه' : 'ة');
                continue;
            }
            // 7. Lowercase ASCII (transliterations / English)
            if (cp >= 'A' && cp <= 'Z')
            {
                sb.Append((char)(cp + 32));
                continue;
            }
            sb.Append(rune.ToString());
        }

        // Final NFC compose for stable persistence/comparison.
        return sb.ToString().Normalize(NormalizationForm.FormC).Trim();
    }
}
