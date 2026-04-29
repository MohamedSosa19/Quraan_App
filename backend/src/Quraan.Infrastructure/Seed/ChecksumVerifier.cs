using System.Security.Cryptography;

namespace Quraan.Infrastructure.Seed;

/// <summary>
/// Implements Constitution Principle I checksum verification (T045 / T055a).
/// Compares SHA-256 of every file listed in <c>expected.sha256</c> against the
/// stored digest; throws on any mismatch. Used both at seed time and as the
/// boot-time integrity gate (<c>Program.cs</c>).
/// </summary>
public sealed class ChecksumVerifier
{
    public sealed record VerificationResult(bool Ok, IReadOnlyList<string> Errors);

    private readonly string _sourcesDir;

    public ChecksumVerifier(string sourcesDir) => _sourcesDir = sourcesDir;

    /// <summary>Default location: <c>{Quraan.Infrastructure}/Seed/Sources</c>.</summary>
    public static ChecksumVerifier ForAssemblyLocation()
    {
        var asmDir = Path.GetDirectoryName(typeof(ChecksumVerifier).Assembly.Location)!;
        // bin/Debug/net8.0/ → walk up 3 levels to project, then into Seed/Sources.
        var projectDir = Directory.GetParent(asmDir)?.Parent?.Parent?.FullName ?? asmDir;
        return new ChecksumVerifier(Path.Combine(projectDir, "Seed", "Sources"));
    }

    public Task<VerificationResult> VerifyAllAsync(CancellationToken ct = default)
    {
        var manifestPath = Path.Combine(_sourcesDir, "expected.sha256");
        if (!File.Exists(manifestPath))
            return Task.FromResult(new VerificationResult(false, [$"Manifest missing: {manifestPath}"]));

        var errors = new List<string>();
        foreach (var line in File.ReadAllLines(manifestPath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            var parts = trimmed.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                errors.Add($"Malformed manifest line: '{line}'");
                continue;
            }
            var (expectedDigest, fileName) = (parts[0], parts[1].TrimStart('*').Trim());
            if (string.Equals(expectedDigest, "PLACEHOLDER", StringComparison.Ordinal))
            {
                errors.Add($"{fileName}: digest is PLACEHOLDER — populate expected.sha256 (see Seed/Sources/README.md).");
                continue;
            }
            var path = Path.Combine(_sourcesDir, fileName);
            if (!File.Exists(path))
            {
                errors.Add($"{fileName}: missing on disk at {path}");
                continue;
            }
            var actual = ComputeSha256(path);
            if (!string.Equals(actual, expectedDigest, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{fileName}: digest mismatch (expected {expectedDigest}, got {actual})");
            }
        }

        return Task.FromResult(new VerificationResult(errors.Count == 0, errors));
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string SourcesDirectory => _sourcesDir;
}
