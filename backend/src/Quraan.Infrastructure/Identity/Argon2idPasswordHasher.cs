using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace Quraan.Infrastructure.Identity;

/// <summary>
/// Argon2id password hasher per R-06 / Constitution V.
/// Format: <c>$argon2id$v=19$m=19456,t=2,p=1$&lt;salt-b64&gt;$&lt;hash-b64&gt;</c>.
/// </summary>
public sealed class Argon2idPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
{
    private const int MemoryKiB = 19_456;     // 19 MiB
    private const int Iterations = 2;
    private const int Parallelism = 1;
    private const int SaltLength = 32;
    private const int HashLength = 32;
    private const string Tag = "$argon2id$v=19$";

    public string HashPassword(TUser user, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Compute(password, salt);
        return $"{Tag}m={MemoryKiB},t={Iterations},p={Parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword) || !hashedPassword.StartsWith(Tag, StringComparison.Ordinal))
            return PasswordVerificationResult.Failed;

        var parts = hashedPassword.Split('$');
        // Expected: ["", "argon2id", "v=19", "m=...,t=...,p=...", "<salt>", "<hash>"]
        if (parts.Length != 6) return PasswordVerificationResult.Failed;

        try
        {
            var salt = Convert.FromBase64String(parts[4]);
            var expected = Convert.FromBase64String(parts[5]);
            var actual = Compute(providedPassword, salt);
            return CryptographicOperations.FixedTimeEquals(expected, actual)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
        catch
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private static byte[] Compute(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Parallelism,
            MemorySize = MemoryKiB,
            Iterations = Iterations,
        };
        return argon2.GetBytes(HashLength);
    }
}
