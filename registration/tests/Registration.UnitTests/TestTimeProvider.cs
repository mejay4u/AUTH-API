namespace Registration.UnitTests;

/// <summary>A controllable <see cref="TimeProvider"/> for deterministic OTP timing tests.</summary>
internal sealed class TestTimeProvider : TimeProvider
{
    // Anchored near real "now" so MemoryCache's real-clock eviction keeps entries alive for the test's
    // duration, while the service's own expiry logic uses this (advanceable) clock.
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now = Now.Add(by);
}
