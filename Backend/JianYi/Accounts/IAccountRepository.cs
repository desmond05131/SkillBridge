namespace SkillBridge.Web.Features.Accounts;

public interface IAccountRepository
{
    Task<AccountLookup> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<AccountLookup> FindByIdAsync(long userId, CancellationToken cancellationToken);
    Task<AccountUser> CreateAsync(string displayName, string email, string normalizedEmail, string passwordHash, UserRole role, string securityStamp, CancellationToken cancellationToken);
    Task<AccountUser> UpdateProfileAsync(AccountUser user, string displayName, string email, string normalizedEmail, string securityStamp, CancellationToken cancellationToken);
    Task<AccountUser> UpdatePasswordAsync(AccountUser user, string passwordHash, string securityStamp, CancellationToken cancellationToken);
}

public enum PasswordVerification
{
    Failed,
    Success,
    RehashNeeded
}

public interface IPasswordService
{
    string Hash(string password);
    PasswordVerification Verify(string passwordHash, string password);
}
