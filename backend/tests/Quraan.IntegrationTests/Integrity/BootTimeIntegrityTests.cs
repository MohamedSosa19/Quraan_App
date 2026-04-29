using FluentAssertions;
using Quraan.Infrastructure.Seed;
using Xunit;

namespace Quraan.IntegrationTests.Integrity;

/// <summary>
/// T055b — verifies the Principle I "fail closed on every read path" gate.
/// We cannot easily simulate the full host-start failure path inside an in-process
/// test; instead we assert the verifier itself behaves correctly on:
///   1. PLACEHOLDER manifest entries → reports an actionable error.
///   2. Missing source file → reports an actionable error.
///   3. Wrong digest → reports a mismatch.
///   4. Correct digest → reports OK.
/// </summary>
public class BootTimeIntegrityTests : IDisposable
{
    private readonly string _tempDir;

    public BootTimeIntegrityTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "quraan-integrity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }

    [Fact]
    public async Task Placeholder_digest_is_rejected()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "expected.sha256"),
            "PLACEHOLDER  somefile.txt\n");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "somefile.txt"), "irrelevant");
        var verifier = new ChecksumVerifier(_tempDir);
        var result = await verifier.VerifyAllAsync();
        result.Ok.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("PLACEHOLDER", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Missing_file_is_rejected()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "expected.sha256"),
            "0000000000000000000000000000000000000000000000000000000000000000  missing.txt\n");
        var verifier = new ChecksumVerifier(_tempDir);
        var result = await verifier.VerifyAllAsync();
        result.Ok.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("missing on disk", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Digest_mismatch_is_rejected()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "expected.sha256"),
            "0000000000000000000000000000000000000000000000000000000000000000  data.txt\n");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "data.txt"), "real bytes");
        var verifier = new ChecksumVerifier(_tempDir);
        var result = await verifier.VerifyAllAsync();
        result.Ok.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("digest mismatch", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Correct_digest_passes()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("hello world");
        var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
        await File.WriteAllBytesAsync(Path.Combine(_tempDir, "data.txt"), bytes);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "expected.sha256"),
            $"{digest}  data.txt\n");
        var verifier = new ChecksumVerifier(_tempDir);
        var result = await verifier.VerifyAllAsync();
        result.Ok.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
