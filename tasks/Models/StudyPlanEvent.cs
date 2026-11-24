using System.ComponentModel.DataAnnotations;

namespace StudyFlow.Models;

public class StudyPlanEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;

    public string? CourseId { get; set; }

    [Required(ErrorMessage = "Event title is required.")]
    [StringLength(140, MinimumLength = 2, ErrorMessage = "Title must be 2–140 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    public StudyPlanType Type { get; set; } = StudyPlanType.StudySession;

    public DateTime Start { get; set; } = DateTime.Today.AddHours(9);
    public DateTime End { get; set; } = DateTime.Today.AddHours(10);

    [StringLength(120)]
    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
