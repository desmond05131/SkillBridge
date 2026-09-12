namespace SkillBridge.Web.Features.Accounts;

public enum UserRole
{
    Member,
    Admin
}

public sealed record AccountUser(long Id, string DisplayName, string Email, UserRole Role, bool IsActive, string SecurityStamp);

public sealed record AccountCredentials(AccountUser User, string PasswordHash);

public abstract record AccountLookup
{
    public sealed record Found(AccountCredentials Credentials) : AccountLookup;
    public sealed record Missing : AccountLookup;
}

public abstract record AuthenticationResult
{
    public sealed record Authenticated(AccountUser User) : AuthenticationResult;
    public sealed record Rejected : AuthenticationResult;
}

public sealed class DuplicateEmailException(Exception innerException) : Exception("An account with this email already exists.", innerException);

public sealed class AccountChangedException() : Exception("Your account changed. Sign in again before continuing.");

public sealed class IncorrectPasswordException() : Exception("The current password is incorrect.");
