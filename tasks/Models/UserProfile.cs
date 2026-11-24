using System.ComponentModel.DataAnnotations;

namespace StudyFlow.Models;

public class UserProfile
{
    public string Id { get; set; } = "demo-user";

    [Required(ErrorMessage = "Your name is required.")]
    [StringLength(80, ErrorMessage = "Name cannot exceed 80 characters.")]
    public string FullName { get; set; } = "Mohamed Sicid Muse";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(160)]
    public string Email { get; set; } = "alex.rivera@university.edu";

    [StringLength(120)]
    public string University { get; set; } = "Global Science University";

    [StringLength(120)]
    public string Program { get; set; } = "B.Sc. Information Technology";

    [StringLength(40)]
    public string YearOfStudy { get; set; } = "Year 4";

    [StringLength(400)]
    public string? Bio { get; set; } = "IT student at Global Science University (GSU) specializing in software engineering, networks, and enterprise systems.";

    public string AvatarColor { get; set; } = "#4f46e5";

    [Range(15, 600, ErrorMessage = "Daily study goal must be between 15 and 600 minutes.")]
    public int DailyGoalMinutes { get; set; } = 120;

    [Range(1, 120, ErrorMessage = "Focus duration must be between 1 and 120 minutes.")]
    public int FocusMinutes { get; set; } = 25;

    [Range(1, 60, ErrorMessage = "Short break must be between 1 and 60 minutes.")]
    public int ShortBreakMinutes { get; set; } = 5;

    [Range(1, 90, ErrorMessage = "Long break must be between 1 and 90 minutes.")]
    public int LongBreakMinutes { get; set; } = 15;

    [Range(2, 8, ErrorMessage = "Long break interval must be between 2 and 8 sessions.")]
    public int LongBreakInterval { get; set; } = 4;

    public bool SoundEnabled { get; set; } = true;
    public bool AutoStartBreaks { get; set; } = true;

    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "SF",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
            };
        }
    }
}
