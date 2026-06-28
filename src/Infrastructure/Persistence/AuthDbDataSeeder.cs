using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthApi.Infrastructure.Persistence;

/// <summary>
/// Seeds mock members/LOBs/plans into the (InMemory) database so the API is runnable end-to-end
/// without the real member database. This runs only when seeding is enabled and the store is empty,
/// and is a no-op against a real SQL Server unless you explicitly opt in.
///
/// Demo credentials (Development only):
///   username: jdoe   password: P@ssw0rd!   -> LOBs: DENTAL, VISION   Plans: 1001, 2001
///   username: asmith password: Secret123!  -> LOBs: MEDICAL          Plans: 3001
/// </summary>
public sealed class AuthDbDataSeeder(
    AuthDbContext context,
    IPasswordHasher passwordHasher,
    ILogger<AuthDbDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Members.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding mock member data...");

        var dental = new Lob { Id = 1, Code = "DENTAL", Name = "Dental" };
        var vision = new Lob { Id = 2, Code = "VISION", Name = "Vision" };
        var medical = new Lob { Id = 3, Code = "MEDICAL", Name = "Medical" };
        context.Lobs.AddRange(dental, vision, medical);

        var planDental = new Plan { Id = 1001, Code = "DEN-PPO", Name = "Dental PPO", LobId = 1 };
        var planVision = new Plan { Id = 2001, Code = "VIS-STD", Name = "Vision Standard", LobId = 2 };
        var planMedical = new Plan { Id = 3001, Code = "MED-HMO", Name = "Medical HMO", LobId = 3 };
        context.Plans.AddRange(planDental, planVision, planMedical);

        context.Members.Add(CreateMember(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "jdoe", "P@ssw0rd!", "john.doe@example.com", "John", "Doe",
            lobIds: [1, 2], planIds: [1001, 2001]));

        context.Members.Add(CreateMember(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "asmith", "Secret123!", "amy.smith@example.com", "Amy", "Smith",
            lobIds: [3], planIds: [3001]));

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Mock member data seeded.");
    }

    private Member CreateMember(
        Guid id, string username, string password, string email, string first, string last,
        int[] lobIds, int[] planIds)
    {
        var (hash, salt) = passwordHasher.Hash(password);

        return new Member
        {
            Id = id,
            Username = username,
            Email = email,
            FirstName = first,
            LastName = last,
            IsActive = true,
            PasswordHash = hash,
            PasswordSalt = salt,
            MemberLobs = lobIds.Select(lid => new MemberLob { MemberId = id, LobId = lid }).ToList(),
            MemberPlans = planIds.Select(pid => new MemberPlan { MemberId = id, PlanId = pid }).ToList()
        };
    }
}
