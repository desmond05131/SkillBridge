using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Accounts;
using SkillBridge.Web.Infrastructure.Authentication;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Pages.Account;

[AllowAnonymous]
public sealed class RegisterModel(AccountService accounts) : PageModel
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
        if (User.Identity?.IsAuthenticated is true)
            return LocalRedirect(ReturnUrl ?? "/");

        if (!ModelState.IsValid)
            return InvalidPage();

        try
        {
            var user = await accounts.RegisterAsync(Input.DisplayName, Input.Email, Input.Password, cancellationToken);
            await AccountSession.SignInAsync(HttpContext, user);
            return LocalRedirect(ReturnUrl ?? "/");
        }
        catch (DuplicateEmailException)
        {
            ModelState.AddModelError("Input.Email", "This email is already registered. Sign in or use another email.");
            return InvalidPage();
        }
        catch (DatabaseUnavailableException)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ModelState.AddModelError(string.Empty, "Registration is temporarily unavailable. Please try again shortly.");
            return InvalidPage();
        }
    }

    private PageResult InvalidPage()
    {
        Input.Password = string.Empty;
        Input.ConfirmPassword = string.Empty;
        ModelState.SetModelValue("Input.Password", string.Empty, string.Empty);
        ModelState.SetModelValue("Input.ConfirmPassword", string.Empty, string.Empty);
        return Page();
    }

    public sealed class InputModel
    {
        [Required, StringLength(80, MinimumLength = 2), Display(Name = "Display name")]
        public string DisplayName { get; set => field = value?.Trim() ?? string.Empty; } = string.Empty;

        [Required, EmailAddress, StringLength(254), Display(Name = "Email address")]
        public string Email { get; set => field = value?.Trim() ?? string.Empty; } = string.Empty;

        [Required, StringLength(AccountService.MaximumPasswordLength, MinimumLength = AccountService.MinimumPasswordLength), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password), ErrorMessage = "The passwords do not match."), DataType(DataType.Password), Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
