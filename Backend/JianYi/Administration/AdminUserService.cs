using System.ComponentModel.DataAnnotations;
using SkillBridge.Web.Features.Accounts;

namespace SkillBridge.Web.Features.Administration;

public sealed class AdminUserService(IAdminUserStore store, IPasswordService passwords)
{
    public const int MaximumSearchLength = 100;

    public Task<AdminUserPage> SearchAsync(AdminUserSearch search, CancellationToken cancellationToken)
    {
        var query = search.Query.Trim();
        if (query.Length > MaximumSearchLength)
            throw new ValidationException($"Search must be {MaximumSearchLength} characters or fewer.");

        if (search.Role is { } role && !Enum.IsDefined(role))
            throw new ValidationException("Choose Member or Admin.");

        return store.SearchAsync(search with { Query = query, Page = Math.Max(1, search.Page) }, cancellationToken);
    }

    public Task<AdminUser> GetAsync(long userId, CancellationToken cancellationToken) => store.GetAsync(userId, cancellationToken);

    public Task<AdminUser> CreateAsync(long actingAdminId, AdminUserInput input, string password, CancellationToken cancellationToken)
    {
        var validatedInput = Validate(input);
        PasswordPolicy.Validate(password);

        // Store a one-way hash, never the password typed into the form.
        return store.CreateAsync(actingAdminId, validatedInput, AccountService.NormalizeEmail(validatedInput.Email),
            passwords.Hash(password), NewStamp(), cancellationToken);
    }

    public Task<AdminUser> UpdateAsync(long actingAdminId, long userId, string expectedStamp, AdminUserInput input, CancellationToken cancellationToken)
    {
        var validatedInput = Validate(input);
        ValidateStamp(expectedStamp);

        // A new stamp signs out older sessions and also makes older edit forms out of date.
        return store.UpdateAsync(actingAdminId, userId, expectedStamp, validatedInput,
            AccountService.NormalizeEmail(validatedInput.Email), NewStamp(), cancellationToken);
    }

    public Task DeleteAsync(long actingAdminId, long userId, string expectedStamp, CancellationToken cancellationToken)
    {
        ValidateStamp(expectedStamp);
        return store.DeleteAsync(actingAdminId, userId, expectedStamp, cancellationToken);
    }

    private static AdminUserInput Validate(AdminUserInput input)
    {
        var normalized = input with { DisplayName = input.DisplayName.Trim(), Email = input.Email.Trim() };
        Validator.ValidateObject(normalized, new ValidationContext(normalized), validateAllProperties: true);
        return normalized;
    }

    private static void ValidateStamp(string expectedStamp)
    {
        if (!Guid.TryParseExact(expectedStamp, "N", out _))
            throw new AdminUserChangedException();
    }

    private static string NewStamp() => Guid.NewGuid().ToString("N");
}
