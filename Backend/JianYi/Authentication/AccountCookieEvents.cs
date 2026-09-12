using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MySqlConnector;
using SkillBridge.Web.Features.Accounts;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Authentication;

public sealed class AccountCookieEvents(IAccountRepository repository, ILogger<AccountCookieEvents> logger) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal is null || !long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
        {
            await RejectAsync(context);
            return;
        }

        try
        {
            var lookup = await repository.FindByIdAsync(userId, context.HttpContext.RequestAborted);
            if (lookup is not AccountLookup.Found found || !found.Credentials.User.IsActive)
            {
                await RejectAsync(context);
                return;
            }

            var user = found.Credentials.User;
            if (principal.FindFirstValue(AccountSession.SecurityStampClaim) != user.SecurityStamp || !principal.IsInRole(user.Role.ToString()))
            {
                await RejectAsync(context);
                return;
            }
        }
        catch (DatabaseUnavailableException)
        {
            logger.LogWarning("Session validation could not reach the database; access was rejected.");
            await RejectAsync(context);
        }
        catch (MySqlException)
        {
            logger.LogWarning("Session validation encountered a database error; access was rejected.");
            await RejectAsync(context);
        }
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
