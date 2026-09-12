namespace SkillBridge.Web.Features.Accounts;

public sealed class AccountService(IAccountRepository repository, IPasswordService passwords)
{
    public const int MinimumPasswordLength = 12;
    public const int MaximumPasswordLength = 128;

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public Task<AccountUser> RegisterAsync(string displayName, string email, string password, CancellationToken cancellationToken) =>
        CreateAsync(displayName, email, password, UserRole.Member, cancellationToken);

    public Task<AccountUser> CreateAdminAsync(string displayName, string email, string password, CancellationToken cancellationToken) =>
        CreateAsync(displayName, email, password, UserRole.Admin, cancellationToken);

    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
    {
        var lookup = await repository.FindByEmailAsync(NormalizeEmail(email), cancellationToken);
        if (lookup is not AccountLookup.Found found || !found.Credentials.User.IsActive)
            return new AuthenticationResult.Rejected();

        var verification = passwords.Verify(found.Credentials.PasswordHash, password);
        if (verification is PasswordVerification.Failed)
            return new AuthenticationResult.Rejected();

        if (verification is not PasswordVerification.RehashNeeded)
            return new AuthenticationResult.Authenticated(found.Credentials.User);

        var user = await repository.UpdatePasswordAsync(found.Credentials.User, passwords.Hash(password), NewStamp(), cancellationToken);
        return new AuthenticationResult.Authenticated(user);
    }

    public async Task<AccountUser> GetActiveAsync(long userId, CancellationToken cancellationToken)
    {
        var lookup = await repository.FindByIdAsync(userId, cancellationToken);
        if (lookup is not AccountLookup.Found found || !found.Credentials.User.IsActive)
            throw new AccountChangedException();

        return found.Credentials.User;
    }

    public Task<AccountUser> UpdateProfileAsync(AccountUser user, string displayName, string email, CancellationToken cancellationToken) =>
        repository.UpdateProfileAsync(user, displayName.Trim(), email.Trim(), NormalizeEmail(email), NewStamp(), cancellationToken);

    public async Task<AccountUser> ChangePasswordAsync(AccountUser user, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var lookup = await repository.FindByIdAsync(user.Id, cancellationToken);
        if (lookup is not AccountLookup.Found found || !found.Credentials.User.IsActive || found.Credentials.User.SecurityStamp != user.SecurityStamp)
            throw new AccountChangedException();

        if (passwords.Verify(found.Credentials.PasswordHash, currentPassword) is PasswordVerification.Failed)
            throw new IncorrectPasswordException();

        return await repository.UpdatePasswordAsync(user, passwords.Hash(newPassword), NewStamp(), cancellationToken);
    }

    private Task<AccountUser> CreateAsync(string displayName, string email, string password, UserRole role, CancellationToken cancellationToken) =>
        repository.CreateAsync(displayName.Trim(), email.Trim(), NormalizeEmail(email), passwords.Hash(password), role, NewStamp(), cancellationToken);

    private static string NewStamp() => Guid.NewGuid().ToString("N");
}
