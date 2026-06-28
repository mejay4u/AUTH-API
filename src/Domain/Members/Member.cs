namespace AuthApi.Domain.Members;

/// <summary>
/// A portal member. Maps to the existing member/user table in the database.
/// The password is verified against a stored salted hash — the API never stores or sees plaintext.
/// Behaviour (lockout bookkeeping) lives on the entity to keep the handler thin.
/// </summary>
public class Member
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>Salted hash of the password, encoded as configured (hex or base64).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Per-user salt, encoded as configured (hex or base64).</summary>
    public string PasswordSalt { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>When false the member cannot authenticate regardless of credentials.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Consecutive failed login attempts; reset on a successful login.</summary>
    public int AccessFailedCount { get; set; }

    /// <summary>If set and in the future, the account is temporarily locked.</summary>
    public DateTime? LockoutEndUtc { get; set; }

    public DateTime? LastLoginUtc { get; set; }

    // Navigation
    public ICollection<MemberLob> MemberLobs { get; set; } = new List<MemberLob>();
    public ICollection<MemberPlan> MemberPlans { get; set; } = new List<MemberPlan>();

    public bool IsLockedOut(DateTime utcNow) => LockoutEndUtc.HasValue && LockoutEndUtc.Value > utcNow;

    /// <summary>Record a failed attempt and lock the account once the threshold is reached.</summary>
    public void RegisterFailedLogin(int maxAttempts, TimeSpan lockoutDuration, DateTime utcNow)
    {
        AccessFailedCount++;
        if (AccessFailedCount >= maxAttempts)
        {
            LockoutEndUtc = utcNow.Add(lockoutDuration);
            AccessFailedCount = 0;
        }
    }

    /// <summary>Clear lockout bookkeeping after a successful authentication.</summary>
    public void RegisterSuccessfulLogin(DateTime utcNow)
    {
        AccessFailedCount = 0;
        LockoutEndUtc = null;
        LastLoginUtc = utcNow;
    }

    public IReadOnlyCollection<string> LobCodes() =>
        MemberLobs.Where(ml => ml.Lob is not null)
                  .Select(ml => ml.Lob.Code)
                  .Distinct()
                  .ToArray();

    public IReadOnlyCollection<int> PlanIds() =>
        MemberPlans.Select(mp => mp.PlanId)
                   .Distinct()
                   .ToArray();
}
