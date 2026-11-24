using System.ComponentModel.DataAnnotations;
using StudyFlow.Models;

namespace StudyFlow.ViewModels;

public class TaskFilterViewModel
{
    public string? Q { get; set; }
    public string? CourseId { get; set; }
    public AcademicTaskStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public AcademicTaskType? Type { get; set; }
    public bool OverdueOnly { get; set; }
    public string Sort { get; set; } = "due";

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Q) || !string.IsNullOrWhiteSpace(CourseId) ||
        Status.HasValue || Priority.HasValue || Type.HasValue || OverdueOnly ||
        Sort != "due";
}

public class TaskCountsViewModel
{
    public int All { get; set; }
    public int ToDo { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public int Overdue { get; set; }
}

public class TaskListViewModel
{
    public IReadOnlyList<TaskListItemViewModel> Tasks { get; set; } = [];
    public IReadOnlyList<Course> Courses { get; set; } = [];
    public TaskFilterViewModel Filters { get; set; } = new();
    public TaskCountsViewModel Counts { get; set; } = new();
}

public class TaskFormViewModel : IValidatableObject
{
    public string? Id { get; set; }
    public bool IsEdit => !string.IsNullOrEmpty(Id);
    public string? ReturnUrl { get; set; }

    [Required(ErrorMessage = "Task title is required.")]
    [StringLength(140, MinimumLength = 3, ErrorMessage = "Title must be 3–140 characters.")]
    [Display(Name = "Task title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    [Display(Name = "Course")]
    public string? CourseId { get; set; }

    [Display(Name = "Task type")]
    public AcademicTaskType Type { get; set; } = AcademicTaskType.Assignment;

    [Display(Name = "Priority")]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [Display(Name = "Status")]
    public AcademicTaskStatus Status { get; set; } = AcademicTaskStatus.ToDo;

    [Required(ErrorMessage = "Start date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Due date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Due date")]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);

    [Range(5, 1440, ErrorMessage = "Estimated duration must be between 5 and 1440 minutes.")]
    [Display(Name = "Estimated study time (minutes)")]
    public int EstimatedMinutes { get; set; } = 60;

    public IReadOnlyList<Course> Courses { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueDate.Date < StartDate.Date)
        {
            yield return new ValidationResult(
                "The due date cannot be earlier than the start date.",
                [nameof(DueDate)]);
        }
    }

    public static TaskFormViewModel FromTask(AcademicTask task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        CourseId = task.CourseId,
        Type = task.Type,
        Priority = task.Priority,
        Status = task.Status,
        StartDate = task.StartDate,
        DueDate = task.DueDate,
        EstimatedMinutes = task.EstimatedMinutes
    };
}

public class TaskDetailsViewModel
{
    public AcademicTask Task { get; set; } = new();
    public Course? Course { get; set; }
    public int FocusMinutes { get; set; }
    public int FocusSessions { get; set; }
    public IReadOnlyList<PomodoroSession> Sessions { get; set; } = [];
    public AcademicTask? NextTask { get; set; }
}
