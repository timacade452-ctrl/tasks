namespace StudyFlow.Models;

public class PomodoroSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string? CourseId { get; set; }
    public string? TaskId { get; set; }
    public PomodoroPhase Phase { get; set; } = PomodoroPhase.Focus;
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int DurationMinutes { get; set; }
}
