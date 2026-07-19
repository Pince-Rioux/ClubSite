using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ClubSite.Models;
using ClubSite.Services;

namespace ClubSite.Pages;

[Authorize]
[AutoValidateAntiforgeryToken]
public class MemberProfileModel : PageModel
{
    private readonly IMembershipService _membershipService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<MemberProfileModel> _logger;

    public MemberProfileModel(
        IMembershipService membershipService,
        UserManager<User> userManager,
        ILogger<MemberProfileModel> logger)
    {
        _membershipService = membershipService;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string MembershipNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MemberSince { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public class InputModel
    {
        [Display(Name = "Vorname")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Display(Name = "Nachname")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Telefonnummer ist erforderlich")]
        [Phone(ErrorMessage = "Ungültige Telefonnummer")]
        [Display(Name = "Telefon")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adresse ist erforderlich")]
        [Display(Name = "Adresse")]
        [StringLength(200)]
        public string HomeAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Stadt ist erforderlich")]
        [Display(Name = "Stadt")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bundesland ist erforderlich")]
        [Display(Name = "Bundesland")]
        [StringLength(50)]
        public string State { get; set; } = "Bayern";

        [Required(ErrorMessage = "PLZ ist erforderlich")]
        [Display(Name = "PLZ")]
        [StringLength(10)]
        public string ZipCode { get; set; } = string.Empty;

        [Display(Name = "Notfallkontakt")]
        [StringLength(500)]
        public string? EmergencyContact { get; set; }

        [Display(Name = "Besondere Bedürfnisse")]
        [StringLength(500)]
        public string? SpecialNeeds { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        await LoadUserData(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        try
        {
            var profile = new MemberProfile
            {
                UserId = user.Id,
                HomeAddress = Input.HomeAddress,
                Phone = Input.Phone,
                City = Input.City,
                State = Input.State,
                ZipCode = Input.ZipCode,
                EmergencyContactInfo = Input.EmergencyContact,
                SpecialNeeds = Input.SpecialNeeds,
                ProfileUpdated = System.DateTime.UtcNow
            };

            var result = await _membershipService.UpdateMemberProfileAsync(profile);

            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Fehler beim Speichern des Profils.");
                return Page();
            }

            // Update user display name if provided
            if (!string.IsNullOrWhiteSpace(Input.FirstName) || !string.IsNullOrWhiteSpace(Input.LastName))
            {
                user.UserName = $"{Input.FirstName} {Input.LastName}".Trim();
                await _userManager.UpdateAsync(user);
            }

            TempData["SuccessMessage"] = "Profil erfolgreich aktualisiert.";
            return RedirectToPage();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, "Ein unerwarteter Fehler ist aufgetreten.");
            return Page();
        }
    }

    private async Task LoadUserData(User user)
    {
        Email = user.Email ?? string.Empty;
        MembershipNumber = user.MembershipNumber;

        var membership = await _membershipService.GetUserWithMembershipAsync(user.Id);
        if (membership != null)
        {
            var activeMembership = membership.Memberships?.FirstOrDefault(m => m.Status == "Active");
            if (activeMembership != null)
            {
                MemberSince = activeMembership.JoinDate.ToString("dd.MM.yyyy");
                Status = activeMembership.Status == "Active" ? "Aktiv" : activeMembership.Status;
            }

            var profile = membership.Profile;
            if (profile != null)
            {
                Input.HomeAddress = profile.HomeAddress;
                Input.Phone = profile.Phone;
                Input.City = profile.City;
                Input.State = profile.State;
                Input.ZipCode = profile.ZipCode;
                Input.EmergencyContact = profile.EmergencyContactInfo;
                Input.SpecialNeeds = profile.SpecialNeeds;
            }
        }

        // Split display name
        var parts = (user.UserName ?? "").Split(' ', 2);
        Input.FirstName = parts.Length > 0 ? parts[0] : string.Empty;
        Input.LastName = parts.Length > 1 ? parts[1] : string.Empty;
    }
}