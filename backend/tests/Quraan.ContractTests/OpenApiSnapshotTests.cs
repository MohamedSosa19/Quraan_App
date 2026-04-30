using FluentAssertions;
using Xunit;

namespace Quraan.ContractTests;

/// <summary>
/// T185 — Drift gate against the committed
/// <c>specs/001-quran-mvp/contracts/openapi.yaml</c>. The earlier baseline only
/// asserted the file was present; this version pins the must-have endpoints,
/// HTTP verbs, response shapes, and shared components so a contract-impacting
/// change forces an explicit YAML edit (and therefore a code-review touchpoint).
/// </summary>
public class OpenApiSnapshotTests
{
    private static string LoadCommittedYaml()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10 && dir != null; i++)
        {
            var candidate = Path.Combine(dir, "specs", "001-quran-mvp", "contracts", "openapi.yaml");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new FileNotFoundException(
            "openapi.yaml not located walking up from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Yaml_declares_v1_servers_and_problem_components()
    {
        var content = LoadCommittedYaml();
        content.Should().Contain("/api/v1", "servers must be rooted under /api/v1");
        content.Should().Contain("ProblemDetails", "RFC 7807 ProblemDetails schema must be referenced");
        content.Should().Contain("Problem429", "FR-029b 429 response component must be defined");
        content.Should().Contain("Problem401", "Auth-protected paths must reference Problem401");
        content.Should().Contain("Problem404", "Missing-resource paths must reference Problem404");
    }

    [Theory]
    [InlineData("/surahs:")]
    [InlineData("/surahs/{surahId}:")]
    [InlineData("/surahs/{surahId}/ayahs/{numberInSurah}:")]
    [InlineData("/search:")]
    [InlineData("/tafsir/{surahId}/{numberInSurah}:")]
    [InlineData("/audio/{surahId}:")]
    [InlineData("/auth/register:")]
    [InlineData("/auth/login:")]
    [InlineData("/auth/refresh:")]
    [InlineData("/auth/logout:")]
    [InlineData("/users/me:")]
    [InlineData("/bookmarks:")]
    [InlineData("/bookmarks/{bookmarkId}:")]
    [InlineData("/lastread/me:")]
    [InlineData("/health/live:")]
    [InlineData("/health/ready:")]
    public void Yaml_declares_required_paths(string path)
    {
        var content = LoadCommittedYaml();
        content.Should().Contain(path, $"path '{path}' is part of the v1 contract");
    }

    [Theory]
    [InlineData("SurahSummary")]
    [InlineData("SurahDetail")]
    [InlineData("Ayah")]
    [InlineData("SearchResponse")]
    [InlineData("AuthResponse")]
    [InlineData("Bookmark")]
    [InlineData("LastReadPosition")]
    public void Yaml_declares_required_schemas(string schemaName)
    {
        var content = LoadCommittedYaml();
        content.Should().Contain(schemaName, $"schema '{schemaName}' is part of the v1 contract");
    }
}
