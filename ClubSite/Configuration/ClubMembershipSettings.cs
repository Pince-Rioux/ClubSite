namespace ClubSite.Data;

/// <summary>
/// Configuration settings for membership features.
/// </summary>
public class ClubMembershipSettings
{
    public const string SectionName = "ClubMembership";

    public int DefaultMembershipDurationInMonths { get; set; } = 12;
    public int GracePeriodDays { get; set; } = 30;
    public string WelcomeEmailSubject { get; set; } = "Willkommen bei unserem Club";
    public string RenewalReminderEmailSubject { get; set; } = "Erinnerung: Mitgliedschaft läuft ab";
    public string CancellationEmailSubject { get; set; } = "Mitgliedschaft gekündigt";
    public string AdminEmailAddress { get; set; } = "admin@volleyballclub.de";
    public bool SendWelcomeEmailOnRegistration { get; set; } = true;
    public int RenewalReminderDaysBefore { get; set; } = 30;
}