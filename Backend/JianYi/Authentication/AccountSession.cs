using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SkillBridge.Web.Features.Accounts;

namespace SkillBridge.Web.Infrastructure.Authentication;

public static class AccountSession
{
    public const string SecurityStampClaim = "skillbridge:security_stamp";

    public static ClaimsPrincipal CreatePrincipal(AccountUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(SecurityStampClaim, user.SecurityStamp)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static long UserId(ClaimsPrincipal principal)
    {
        if (!long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
            throw new AccountChangedException();

        return userId;
    }

    public static Task SignInAsync(HttpContext context, AccountUser user) =>
        context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, CreatePrincipal(user),
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
}
