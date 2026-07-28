namespace Registration.UnitTests;

/// <summary>A controllable <see cref="TimeProvider"/> for deterministic expiry tests.</summary>
internal sealed class TestTimeProvider : TimeProvider
{
    // Anchored near real "now"; the code under test reads expiry from this (advanceable) clock.
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now = Now.Add(by);
}
