using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ClubSite.Models;
using ClubSite.Services;

namespace ClubSite.Pages;

[AutoValidateAntiforgeryToken]
public class MemberRegistrationModel : PageModel
{
    private readonly IMembershipService _membershipService;
    private readonly ILogger<MemberRegistrationModel> _logger;

    public MemberRegistrationModel(IMembershipService membershipService, ILogger<MemberRegistrationModel> logger)
    {
        _membershipService = membershipService;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Vorname ist erforderlich")]
        [Display(Name = "Vorname")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nachname ist erforderlich")]
        [Display(Name = "Nachname")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-Mail ist erforderlich")]
        [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse")]
        [Display(Name = "E-Mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passwort ist erforderlich")]
        [StringLength(100, ErrorMessage = "Das Passwort muss mindestens {2} Zeichen lang sein.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Passwort bestätigen")]
        [Compare("Password", ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        public string ConfirmPassword { get; set; } = string.Empty;

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

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var user = new User
            {
                UserName = $"{Input.FirstName} {Input.LastName}".Trim(),
                Email = Input.Email,
                PhoneNumber = Input.Phone,
                MembershipNumber = await _membershipService.GenerateMembershipNumberAsync(),
                StartDate = System.DateTime.UtcNow,
                EndDate = System.DateTime.UtcNow.AddMonths(12),
                EmergencyContact = Input.EmergencyContact,
                SpecialNeeds = Input.SpecialNeeds
            };

            var result = await _membershipService.RegisterMemberAsync(user, Input.Password, "Member");

            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Die Registrierung ist fehlgeschlagen. Möglicherweise ist diese E-Mail bereits registriert.");
                return Page();
            }

            // Save profile
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

            await _membershipService.UpdateMemberProfileAsync(profile);

            TempData["SuccessMessage"] = "Registrierung erfolgreich! Sie erhalten eine Bestätigungs-E-Mail.";
            _logger.LogInformation("New member registered: {Email}", Input.Email);

            return RedirectToPage();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Email}", Input.Email);
            ModelState.AddModelError(string.Empty, "Ein unerwarteter Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.");
            return Page();
        }
    }
}