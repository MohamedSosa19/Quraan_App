using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Quraan.Infrastructure.Identity;
using Xunit;

namespace Quraan.UnitTests.Auth;

public sealed class Argon2idPasswordHasherTests
{
    private sealed class FakeUser { }

    [Fact]
    public void HashPassword_round_trips_a_known_password()
    {
        var hasher = new Argon2idPasswordHasher<FakeUser>();
        var user = new FakeUser();
        var hash = hasher.HashPassword(user, "correct horse battery staple");
        hash.Should().StartWith("$argon2id$v=19$");

        var verify = hasher.VerifyHashedPassword(user, hash, "correct horse battery staple");
        verify.Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void HashPassword_rejects_a_wrong_password()
    {
        var hasher = new Argon2idPasswordHasher<FakeUser>();
        var user = new FakeUser();
        var hash = hasher.HashPassword(user, "correct horse battery staple");

        var verify = hasher.VerifyHashedPassword(user, hash, "incorrect horse");
        verify.Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void HashPassword_rejects_a_tampered_hash()
    {
        var hasher = new Argon2idPasswordHasher<FakeUser>();
        var user = new FakeUser();
        var hash = hasher.HashPassword(user, "correct horse battery staple");
        // Flip the last character of the hash payload.
        var tampered = hash[..^1] + (hash[^1] == 'A' ? 'B' : 'A');

        var verify = hasher.VerifyHashedPassword(user, tampered, "correct horse battery staple");
        verify.Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void HashPassword_rejects_a_malformed_hash()
    {
        var hasher = new Argon2idPasswordHasher<FakeUser>();
        var verify = hasher.VerifyHashedPassword(new FakeUser(), "not-a-real-hash", "any");
        verify.Should().Be(PasswordVerificationResult.Failed);
    }
}
