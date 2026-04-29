using FluentAssertions;
using Xunit;

namespace Quraan.ContractTests;

/// <summary>
/// Drift gate: the runtime-generated OpenAPI document MUST match the committed
/// <c>specs/001-quran-mvp/contracts/openapi.yaml</c>. T056 wires up
/// build-time emission; this test asserts the committed file is present and
/// non-empty as the Phase-2 baseline. The byte-for-byte snapshot comparison
/// is added in T056 once Swashbuckle's CLI emission is wired in.
/// </summary>
public class OpenApiSnapshotTests
{
    [Fact]
    public void Committed_openapi_yaml_exists_and_declares_v1_paths()
    {
        // Walk up from the test bin to the repo root.
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10 && dir != null; i++)
        {
            var candidate = Path.Combine(dir, "specs", "001-quran-mvp", "contracts", "openapi.yaml");
            if (File.Exists(candidate))
            {
                var content = File.ReadAllText(candidate);
                content.Should().Contain("/api/v1");
                content.Should().Contain("ProblemDetails");
                content.Should().Contain("Problem429");
                return;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        Assert.Fail("openapi.yaml not located walking up from " + AppContext.BaseDirectory);
    }
}
