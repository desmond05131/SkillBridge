using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SkillBridge.Web.Features.Accounts;

public static class PasswordPolicy
{
    public const string Pattern = @"(?=[\s\S]*[A-Z])(?=[\s\S]*[\p{P}\p{S}])[\s\S]*";
    public const string Message = "Use 12–128 characters, including at least one uppercase letter (A–Z) and one symbol (for example !, @ or #).";

    public static bool IsValid(string? password) =>
        password is { Length: >= AccountService.MinimumPasswordLength and <= AccountService.MaximumPasswordLength }
        && Regex.IsMatch(password, Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));

    public static void Validate(string password)
    {
        if (!IsValid(password)) throw new ValidationException(Message);
    }
}
