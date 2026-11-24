using System.ComponentModel.DataAnnotations;
using StudyFlow.Models;

namespace StudyFlow.ViewModels;

public class SettingsViewModel
{
    [Range(15, 600, ErrorMessage = "Daily study goal must be between 15 and 600 minutes.")]
    [Display(Name = "Daily study goal (minutes)")]
    public int DailyGoalMinutes { get; set; } = 120;

    [Range(1, 120, ErrorMessage = "Focus duration must be between 1 and 120 minutes.")]
    [Display(Name = "Focus session (minutes)")]
    public int FocusMinutes { get; set; } = 25;

    [Range(1, 60, ErrorMessage = "Short break must be between 1 and 60 minutes.")]
    [Display(Name = "Short break (minutes)")]
    public int ShortBreakMinutes { get; set; } = 5;

    [Range(1, 90, ErrorMessage = "Long break must be between 1 and 90 minutes.")]
    [Display(Name = "Long break (minutes)")]
    public int LongBreakMinutes { get; set; } = 15;

    [Range(2, 8, ErrorMessage = "Long break interval must be between 2 and 8 sessions.")]
    [Display(Name = "Long break after (sessions)")]
    public int LongBreakInterval { get; set; } = 4;

    [Display(Name = "Play a sound when a session ends")]
    public bool SoundEnabled { get; set; } = true;

    [Display(Name = "Automatically start breaks")]
    public bool AutoStartBreaks { get; set; } = true;

    public UserProfile Profile { get; set; } = new();
    public StudySummary Summary { get; set; } = new();
}

public class ProfileViewModel
{
    public UserProfile Profile { get; set; } = new();
    public ProfileFormViewModel Form { get; set; } = new();
    public StudySummary Summary { get; set; } = new();
    public IReadOnlyList<CourseSummaryViewModel> Courses { get; set; } = [];
    public IReadOnlyList<ActivityItem> RecentActivity { get; set; } = [];
    public int MaterialsCount { get; set; }
    public int CompletedTasks { get; set; }
}

public class ProfileFormViewModel
{
    [Required(ErrorMessage = "Your name is required.")]
    [StringLength(80, ErrorMessage = "Name cannot exceed 80 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(160)]
    [Display(Name = "University email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "University")]
    public string University { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Program")]
    public string Program { get; set; } = string.Empty;

    [StringLength(40)]
    [Display(Name = "Year")]
    public string YearOfStudy { get; set; } = string.Empty;

    [StringLength(400, ErrorMessage = "Bio cannot exceed 400 characters.")]
    [Display(Name = "Short bio")]
    public string? Bio { get; set; }

    public static ProfileFormViewModel FromProfile(UserProfile profile) => new()
    {
        FullName = profile.FullName,
        Email = profile.Email,
        University = profile.University,
        Program = profile.Program,
        YearOfStudy = profile.YearOfStudy,
        Bio = profile.Bio
    };
}
