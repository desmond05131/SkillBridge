using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillBridge.Web.Features.Accounts;
using SkillBridge.Web.Features.Administration;
using SkillBridge.Web.Infrastructure.Authentication;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public sealed class EditModel(AdminUserService users) : PageModel
{
    // No id means Create. An id means Edit; both modes use this same page and form.
    [BindProperty(SupportsGet = true)]
    public long? Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsCreate => Id is null;
    public bool IsOwnAccount => Id == AccountSession.UserId(User);
    public bool IsStale { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (Id is <= 0)
            return NotFound();

        if (IsCreate)
            return Page();

        try
        {
            var user = await users.GetAsync(Id!.Value, cancellationToken);
            Input = new InputModel
            {
                DisplayName = user.DisplayName,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                SecurityStamp = user.SecurityStamp
            };
            return Page();
        }
        catch (AdminUserNotFoundException)
        {
            return NotFound();
        }
        catch (DatabaseUnavailableException)
        {
            return DatabaseUnavailablePage();
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Id is <= 0)
            return NotFound();

        // Data annotations validate ordinary fields; these checks depend on Create/Edit mode.
        if (IsCreate && string.IsNullOrEmpty(Input.Password))
            ModelState.AddModelError("Input.Password", "Enter a password for the new user.");

        if (IsCreate && string.IsNullOrEmpty(Input.ConfirmPassword))
            ModelState.AddModelError("Input.ConfirmPassword", "Enter the password again.");

        if (!IsCreate && string.IsNullOrEmpty(Input.SecurityStamp))
            ModelState.AddModelError(string.Empty, "The saved account version is missing. Reload the user before saving.");

        if (!ModelState.IsValid)
            return InvalidPage();

        try
        {
            // The PageModel handles the form; the service handles account rules and saving.
            var input = new AdminUserInput(Input.DisplayName, Input.Email, Input.Role, Input.IsActive);
            var actingAdminId = AccountSession.UserId(User);
            var saved = IsCreate
                ? await users.CreateAsync(actingAdminId, input, Input.Password!, cancellationToken)
                : await users.UpdateAsync(actingAdminId, Id!.Value, Input.SecurityStamp!, input, cancellationToken);

            // Updating an account changes its stamp. Keep our own login in step with that change.
            if (saved.Id == actingAdminId)
                await AccountSession.SignInAsync(HttpContext, new AccountUser(saved.Id, saved.DisplayName, saved.Email, saved.Role, saved.IsActive, saved.SecurityStamp));

            StatusMessage = IsCreate ? "The user account has been created." : "The user account has been saved. Other sessions for this user must sign in again.";
            return RedirectToPage("/JianYi/Admin/Users/Index");
        }
        catch (DuplicateEmailException)
        {
            ModelState.AddModelError("Input.Email", "This email is already registered to another account.");
        }
        catch (AdminUserNotFoundException)
        {
            return NotFound();
        }
        catch (AdminUserChangedException)
        {
            IsStale = true;
            ModelState.AddModelError(string.Empty, "This account changed after you opened it. Your entries are shown below. Copy any changes you need, then reload the user before saving.");
        }
        catch (AdminUserProtectedException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (AdminAccessDeniedException)
        {
            return Forbid();
        }
        catch (ValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (DatabaseUnavailableException)
        {
            return DatabaseUnavailablePage();
        }

        return InvalidPage();
    }

    private PageResult InvalidPage()
    {
        // Keep the entered name/email/role/status, but never send passwords back in the response.
        Input.Password = null;
        Input.ConfirmPassword = null;
        ModelState.SetModelValue("Input.Password", string.Empty, string.Empty);
        ModelState.SetModelValue("Input.ConfirmPassword", string.Empty, string.Empty);
        return Page();
    }

    private PageResult DatabaseUnavailablePage()
    {
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        ModelState.AddModelError(string.Empty, "User management is temporarily unavailable. Please try again shortly.");
        return InvalidPage();
    }

    public sealed class InputModel
    {
        [Required, StringLength(80, MinimumLength = 2), Display(Name = "Full name")]
        public string DisplayName { get; set => field = value?.Trim() ?? string.Empty; } = string.Empty;

        [Required, EmailAddress, StringLength(254), Display(Name = "Email address")]
        public string Email { get; set => field = value?.Trim() ?? string.Empty; } = string.Empty;

        [EnumDataType(typeof(UserRole))]
        public UserRole Role { get; set; } = UserRole.Member;

        public bool IsActive { get; set; } = true;

        [RegularExpression("[0-9a-f]{32}", ErrorMessage = "The saved account version is invalid. Reload the user before saving.")]
        public string? SecurityStamp { get; set; }

        [StringLength(AccountService.MaximumPasswordLength, MinimumLength = AccountService.MinimumPasswordLength), DataType(DataType.Password)]
        [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.Message)]
        public string? Password { get; set; }

        [StringLength(AccountService.MaximumPasswordLength), Compare(nameof(Password), ErrorMessage = "The passwords do not match."), DataType(DataType.Password), Display(Name = "Confirm password")]
        public string? ConfirmPassword { get; set; }
    }
}
