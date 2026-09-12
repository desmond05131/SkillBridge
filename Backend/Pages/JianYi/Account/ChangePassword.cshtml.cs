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
public sealed class ChangePasswordModel(AccountService accounts) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return InvalidPage();

        try
        {
            var current = await accounts.GetActiveAsync(AccountSession.UserId(User), cancellationToken);
            var user = await accounts.ChangePasswordAsync(current, Input.CurrentPassword, Input.NewPassword, cancellationToken);
            await AccountSession.SignInAsync(HttpContext, user);
            StatusMessage = "Your password has been changed. Other sessions must sign in again.";
            return RedirectToPage("/JianYi/Account/Profile");
        }
        catch (IncorrectPasswordException)
        {
            ModelState.AddModelError("Input.CurrentPassword", "The current password is incorrect.");
            return InvalidPage();
        }
        catch (AccountChangedException)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/JianYi/Account/Login", new { ReturnUrl = "/Account/ChangePassword" });
        }
        catch (DatabaseUnavailableException)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ModelState.AddModelError(string.Empty, "The password could not be changed. Please try again shortly.");
            return InvalidPage();
        }
    }

    private PageResult InvalidPage()
    {
        Input = new InputModel();
        foreach (var property in new[] { nameof(Input.CurrentPassword), nameof(Input.NewPassword), nameof(Input.ConfirmPassword) })
            ModelState.SetModelValue($"Input.{property}", string.Empty, string.Empty);

        return Page();
    }

    public sealed class InputModel
    {
        [Required, StringLength(AccountService.MaximumPasswordLength), DataType(DataType.Password), Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, StringLength(AccountService.MaximumPasswordLength, MinimumLength = AccountService.MinimumPasswordLength), DataType(DataType.Password), Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match."), DataType(DataType.Password), Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
