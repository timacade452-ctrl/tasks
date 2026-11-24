using System.ComponentModel.DataAnnotations;

namespace StudyFlow.Models;

public class AcademicTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;

    public string? CourseId { get; set; }

    [Required(ErrorMessage = "Task title is required.")]
    [StringLength(140, MinimumLength = 3, ErrorMessage = "Title must be 3–140 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    public AcademicTaskType Type { get; set; } = AcademicTaskType.Assignment;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public AcademicTaskStatus Status { get; set; } = AcademicTaskStatus.ToDo;

    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);

    [Range(5, 1440, ErrorMessage = "Estimated duration must be between 5 and 1440 minutes.")]
    public int EstimatedMinutes { get; set; } = 60;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public bool IsOverdue =>
        Status != AcademicTaskStatus.Completed && DueDate.Date < DateTime.Today;

    public bool IsDueToday =>
        Status != AcademicTaskStatus.Completed && DueDate.Date == DateTime.Today;
}
