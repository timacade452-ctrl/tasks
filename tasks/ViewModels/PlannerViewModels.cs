using System.ComponentModel.DataAnnotations;
using System.Globalization;
using StudyFlow.Models;

namespace StudyFlow.ViewModels;

public class PlanFormViewModel : IValidatableObject
{
    public string? Id { get; set; }
    public bool IsEdit => !string.IsNullOrEmpty(Id);

    [Required(ErrorMessage = "Event title is required.")]
    [StringLength(140, MinimumLength = 2, ErrorMessage = "Title must be 2–140 characters.")]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes")]
    public string? Description { get; set; }

    [Display(Name = "Event type")]
    public StudyPlanType Type { get; set; } = StudyPlanType.StudySession;

    [Display(Name = "Course")]
    public string? CourseId { get; set; }

    [Required(ErrorMessage = "Choose a date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Start time is required.")]
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Use a valid start time.")]
    [Display(Name = "Start time")]
    public string StartTime { get; set; } = "09:00";

    [Required(ErrorMessage = "End time is required.")]
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Use a valid end time.")]
    [Display(Name = "End time")]
    public string EndTime { get; set; } = "10:00";

    [StringLength(120)]
    [Display(Name = "Location")]
    public string? Location { get; set; }

    public IReadOnlyList<Course> Courses { get; set; } = [];

    public TimeSpan StartSpan => TimeSpan.ParseExact(StartTime, @"hh\:mm", CultureInfo.InvariantCulture);
    public TimeSpan EndSpan => TimeSpan.ParseExact(EndTime, @"hh\:mm", CultureInfo.InvariantCulture);
    public DateTime StartDateTime => Date.Date.Add(StartSpan);
    public DateTime EndDateTime => Date.Date.Add(EndSpan);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TimeSpan.TryParseExact(StartTime, @"hh\:mm", CultureInfo.InvariantCulture, out var start) ||
            !TimeSpan.TryParseExact(EndTime, @"hh\:mm", CultureInfo.InvariantCulture, out var end))
        {
            yield return new ValidationResult("Choose a valid start and end time.", [nameof(StartTime)]);
            yield break;
        }

        if (start < TimeSpan.Zero || start >= TimeSpan.FromDays(1) ||
            end <= TimeSpan.Zero || end > TimeSpan.FromDays(1))
        {
            yield return new ValidationResult("Times must be between 00:00 and 23:59.", [nameof(StartTime)]);
            yield break;
        }

        if (end <= start)
        {
            yield return new ValidationResult("The end time must be after the start time.", [nameof(EndTime)]);
        }
    }

    public static PlanFormViewModel FromPlan(StudyPlanEvent plan) => new()
    {
        Id = plan.Id,
        Title = plan.Title,
        Description = plan.Description,
        Type = plan.Type,
        CourseId = plan.CourseId,
        Date = plan.Start.Date,
        StartTime = plan.Start.ToString("HH:mm", CultureInfo.InvariantCulture),
        EndTime = plan.End.ToString("HH:mm", CultureInfo.InvariantCulture),
        Location = plan.Location
    };
}

public class PlannerDaySummary
{
    public DateTime Date { get; init; }
    public int PlanCount { get; init; }
    public int DeadlineCount { get; init; }
    public bool IsToday { get; init; }
    public bool IsSelected { get; init; }
}

public class PlannerViewModel
{
    public DateTime Date { get; set; } = DateTime.Today;
    public IReadOnlyList<PlanListItemViewModel> Plans { get; set; } = [];
    public IReadOnlyList<TaskListItemViewModel> DueTasks { get; set; } = [];
    public IReadOnlyList<PlannerDaySummary> WeekDays { get; set; } = [];
    public IReadOnlyList<PlanListItemViewModel> Upcoming { get; set; } = [];
    public IReadOnlyList<Course> Courses { get; set; } = [];
    public PlanFormViewModel Form { get; set; } = new();
    public int PlannedMinutes { get; set; }
    public string DateLabel { get; set; } = string.Empty;
    public DateTime PreviousDate { get; set; }
    public DateTime NextDate { get; set; }
}
