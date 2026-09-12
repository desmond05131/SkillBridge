using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Accounts;
using SkillBridge.Web.Infrastructure.Authentication;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Pages.Account;

[Authorize]
public sealed class ProfileModel(AccountService accounts) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var user = await accounts.GetActiveAsync(AccountSession.UserId(User), cancellationToken);
            Input = new InputModel { DisplayName = user.DisplayName, Email = user.Email };
            return Page();
        }
        catch (AccountChangedException)
        {
            return await SignOutAsync();
        }
        catch (DatabaseUnavailableException)
        {
            return DatabaseUnavailablePage();
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var current = await accounts.GetActiveAsync(AccountSession.UserId(User), cancellationToken);
            var user = await accounts.UpdateProfileAsync(current, Input.DisplayName, Input.Email, cancellationToken);
            await AccountSession.SignInAsync(HttpContext, user);
            StatusMessage = "Your profile has been saved. Other sessions must sign in again.";
            return RedirectToPage();
        }
        catch (DuplicateEmailException)
        {
            ModelState.AddModelError("Input.Email", "This email is already registered to another account.");
            return Page();
        }
        catch (AccountChangedException)
        {
            return await SignOutAsync();
        }
        catch (DatabaseUnavailableException)
        {
            return DatabaseUnavailablePage();
        }
    }

    private async Task<IActionResult> SignOutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/JianYi/Account/Login", new { ReturnUrl = "/Account/Profile" });
    }

    private PageResult DatabaseUnavailablePage()
    {
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        ModelState.AddModelError(string.Empty, "Your profile is temporarily unavailable. Please try again shortly.");
        return Page();
    }

    public sealed class InputModel
    {
        [Required, StringLength(80, MinimumLength = 2), Display(Name = "Display name")]
        public string DisplayName { get; set => field = value?.Trim() ?? string.Empty; } = string.Empty;

        [Required, EmailAddress, StringLength(254), Display(Name = "Email address")]
        public string Email { get; set => field = value?.Trim() ?? string.Empty; } = string.Empty;
    }
}
