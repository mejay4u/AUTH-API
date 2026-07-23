using Microsoft.Extensions.Options;
using Registration.Infrastructure.Security.PasswordHashing;
using Xunit;

namespace Registration.UnitTests;

public sealed class Pbkdf2PasswordHasherTests
{
    private static Pbkdf2PasswordHasher CreateHasher() =>
        new(Options.Create(new PasswordHashingOptions { Iterations = 1000 }));

    [Fact]
    public void Hash_then_Verify_succeeds_for_the_same_password()
    {
        var hasher = CreateHasher();

        var (hash, salt) = hasher.Hash("Str0ng!Password");

        Assert.True(hasher.Verify("Str0ng!Password", hash, salt));
    }

    [Fact]
    public void Verify_fails_for_a_different_password()
    {
        var hasher = CreateHasher();

        var (hash, salt) = hasher.Hash("Str0ng!Password");

        Assert.False(hasher.Verify("wrong-password", hash, salt));
    }

    [Fact]
    public void Hash_uses_a_random_salt_so_two_hashes_differ()
    {
        var hasher = CreateHasher();

        var first = hasher.Hash("Str0ng!Password");
        var second = hasher.Hash("Str0ng!Password");

        Assert.NotEqual(first.Salt, second.Salt);
        Assert.NotEqual(first.Hash, second.Hash);
    }

    [Fact]
    public void Verify_returns_false_for_malformed_stored_values()
    {
        var hasher = CreateHasher();

        Assert.False(hasher.Verify("whatever", "not-base64!!", "also-bad"));
    }
}
