using System.ComponentModel.DataAnnotations;
using AgroForum.Constants;
using AgroForum.Helpers;
using AgroForum.Models;
using AgroForum.Services.Recaptcha;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgroForum.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserStore<ApplicationUser> _userStore;
    private readonly IUserEmailStore<ApplicationUser> _emailStore;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IRecaptchaValidator _recaptchaValidator;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger,
        IRecaptchaValidator recaptchaValidator)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore(userManager, userStore);
        _signInManager = signInManager;
        _logger = logger;
        _recaptchaValidator = recaptchaValidator;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string? RecaptchaToken { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

    public class InputModel
    {
        [StringLength(50)]
        [Display(Name = "First name")]
        public string? FirstName { get; set; }

        [StringLength(50)]
        [Display(Name = "Last name")]
        public string? LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var recaptcha = await _recaptchaValidator.ValidateAsync(
            RecaptchaToken,
            RecaptchaActions.Register,
            HttpContext.RequestAborted);
        if (!recaptcha.IsValid)
        {
            ModelState.AddModelError(string.Empty, recaptcha.ErrorMessage!);
            return Page();
        }

        var firstName = NormalizeOptionalName(Input.FirstName);
        var lastName = NormalizeOptionalName(Input.LastName);
        var user = new ApplicationUser
        {
            FirstName = firstName,
            LastName = lastName
        };
        user.DisplayName = CommunityDisplayName.CreateInitial(firstName, lastName, user.Id);

        await _userStore.SetUserNameAsync(user, Input.Email.Trim(), CancellationToken.None);
        await _emailStore.SetEmailAsync(user, Input.Email.Trim(), CancellationToken.None);
        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User created a new account with password.");
            var farmerRoleResult = await _userManager.AddToRoleAsync(user, UserRoles.Farmer);
            if (!farmerRoleResult.Succeeded)
            {
                _logger.LogWarning(
                    "New user {UserId} was created but could not be assigned the default Farmer role. Errors: {Errors}",
                    user.Id,
                    string.Join(", ", farmerRoleResult.Errors.Select(error => error.Code)));
            }

            if (_userManager.Options.SignIn.RequireConfirmedAccount)
            {
                return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    private static string? NormalizeOptionalName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IUserEmailStore<ApplicationUser> GetEmailStore(
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore)
    {
        if (!userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }

        return (IUserEmailStore<ApplicationUser>)userStore;
    }
}
