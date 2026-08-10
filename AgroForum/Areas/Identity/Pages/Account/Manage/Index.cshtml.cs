using System.ComponentModel.DataAnnotations;
using AgroForum.Helpers;
using AgroForum.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgroForum.Areas.Identity.Pages.Account.Manage;

public sealed class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public string Username { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        [Display(Name = "Public display name")]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "First name")]
        public string? FirstName { get; set; }

        [StringLength(50)]
        [Display(Name = "Last name")]
        public string? LastName { get; set; }

        [StringLength(100)]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        [StringLength(250)]
        [Display(Name = "Farming interests")]
        public string? FarmingInterests { get; set; }

        [StringLength(500)]
        [Display(Name = "About you")]
        public string? Bio { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            await LoadIdentityAsync(user);
            return Page();
        }

        user.DisplayName = Input.DisplayName.Trim();
        user.FirstName = Normalize(Input.FirstName);
        user.LastName = Normalize(Input.LastName);
        user.Location = Normalize(Input.Location);
        user.FarmingInterests = Normalize(Input.FarmingInterests);
        user.Bio = Normalize(Input.Bio);

        var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
        if (Input.PhoneNumber != phoneNumber)
        {
            var phoneResult = await _userManager.SetPhoneNumberAsync(user, Normalize(Input.PhoneNumber));
            if (!phoneResult.Succeeded)
            {
                StatusMessage = "Error: Unexpected error when trying to set phone number.";
                return RedirectToPage();
            }
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadIdentityAsync(user);
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your community profile has been updated.";
        return RedirectToPage();
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        await LoadIdentityAsync(user);
        Input = new InputModel
        {
            DisplayName = CommunityDisplayName.For(user),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Location = user.Location,
            FarmingInterests = user.FarmingInterests,
            Bio = user.Bio,
            PhoneNumber = await _userManager.GetPhoneNumberAsync(user)
        };
    }

    private async Task LoadIdentityAsync(ApplicationUser user)
    {
        UserId = user.Id;
        Username = await _userManager.GetUserNameAsync(user) ?? string.Empty;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
