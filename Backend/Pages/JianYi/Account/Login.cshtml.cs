using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Accounts;
using SkillBridge.Web.Infrastructure.Authentication;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel(AccountService accounts) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; } = "/";

    public IActionResult OnGet()
    {
        ReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";
        ModelState.Remove(nameof(ReturnUrl));
        if (User.Identity?.IsAuthenticated is true)
            return LocalRedirect(ReturnUrl ?? "/");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";
        ModelState.Remove(nameof(ReturnUrl));
        if (!ModelState.IsValid)
            return InvalidPage();

        try
        {
            var result = await accounts.AuthenticateAsync(Input.Email, Input.Password, cancellationToken);
            if (result is not AuthenticationResult.Authenticated authenticated)
            {
                ModelState.AddModelError(string.Empty, "The email or password is incorrect, or this account is inactive.");
                return InvalidPage();
            }

            await AccountSession.SignInAsync(HttpContext, authenticated.User);
            return LocalRedirect(ReturnUrl ?? "/");
        }
        catch (DatabaseUnavailableException)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ModelState.AddModelError(string.Empty, "Sign in is temporarily unavailable. Please try again shortly.");
            return InvalidPage();
        }
        catch (AccountChangedException)
        {
            ModelState.AddModelError(string.Empty, "Your account changed during sign in. Please try again.");
            return InvalidPage();
        }
    }

    private PageResult InvalidPage()
    {
        Input.Password = string.Empty;
        ModelState.SetModelValue("Input.Password", string.Empty, string.Empty);
        return Page();
    }

    public sealed class InputModel
    {
        [Required, EmailAddress, StringLength(254), Display(Name = "Email address")]
        public string Email { get; set => field = value?.Trim() ?? string.Empty; } = string.Empty;

        [Required, StringLength(AccountService.MaximumPasswordLength), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
