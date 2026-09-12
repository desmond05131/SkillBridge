using Microsoft.AspNetCore.Identity;
using SkillBridge.Web.Features.Accounts;

namespace SkillBridge.Web.Infrastructure.Authentication;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _hasher = new();
    private readonly object _context = new();

    public string Hash(string password) => _hasher.HashPassword(_context, password);

    public PasswordVerification Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(_context, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.RehashNeeded,
            _ => PasswordVerification.Failed
        };
}
