using FluentAssertions;
using Quraan.Application.Search;
using Xunit;

namespace Quraan.UnitTests.Search;

public class ArabicNormalizerTests
{
    [Fact]
    public void Strips_tashkeel_marks()
    {
        var input = "الرَّحْمَٰنِ"; // contains shadda, fatha, sukun, dagger alif, kasra
        var output = ArabicNormalizer.ForIngest(input);
        output.Should().Be("الرحمن");
    }

    [Fact]
    public void Normalizes_alif_variants_to_bare_alif()
    {
        ArabicNormalizer.ForIngest("أ إ آ ٱ").Should().Be("ا ا ا ا");
    }

    [Fact]
    public void Normalizes_alif_maqsura_to_yaa()
    {
        ArabicNormalizer.ForIngest("على").Should().Be("علي");
    }

    [Fact]
    public void Strips_tatweel()
    {
        ArabicNormalizer.ForIngest("الـسـلـام").Should().Be("السلام");
    }

    [Fact]
    public void Lowercases_ascii_for_query()
    {
        ArabicNormalizer.ForQuery("Mercy").Should().Be("mercy");
    }

    [Fact]
    public void ForIngest_preserves_taa_marbuta()
    {
        ArabicNormalizer.ForIngest("صلاة").Should().Be("صلاة");
    }

    [Fact]
    public void ForQuery_folds_taa_marbuta_to_haa()
    {
        ArabicNormalizer.ForQuery("صلاة").Should().Be("صلاه");
    }

    [Fact]
    public void Empty_input_returns_empty()
    {
        ArabicNormalizer.ForIngest(string.Empty).Should().BeEmpty();
        ArabicNormalizer.ForQuery(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Diacritic_query_matches_diacritic_text_after_normalization()
    {
        var stored = ArabicNormalizer.ForIngest("الرَّحْمَٰنِ");
        var query = ArabicNormalizer.ForQuery("الرحمن");
        stored.Should().Contain(query);
    }
}
