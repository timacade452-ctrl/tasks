using StudyFlow.Models;

namespace StudyFlow.ViewModels;

public class PomodoroSessionViewModel
{
    public PomodoroSession Session { get; init; } = new();
    public Course? Course { get; init; }
    public string? TaskTitle { get; init; }
}

public class PomodoroViewModel
{
    public UserProfile Profile { get; set; } = new();
    public IReadOnlyList<Course> Courses { get; set; } = [];
    public IReadOnlyList<TaskListItemViewModel> ActiveTasks { get; set; } = [];
    public IReadOnlyList<PomodoroSessionViewModel> RecentSessions { get; set; } = [];
    public StudySummary Summary { get; set; } = new();
    public int TodayPlannedMinutes { get; set; }
}

public class RecordPomodoroRequest
{
    public string? CourseId { get; set; }
    public string? TaskId { get; set; }
    public int Minutes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class PomodoroRecordResult
{
    public int TodayFocusMinutes { get; set; }
    public int TodaySessions { get; set; }
    public int TotalFocusMinutes { get; set; }
    public int Streak { get; set; }
    public string? CourseName { get; set; }
    public string? TaskTitle { get; set; }
    public string CompletedAtLabel { get; set; } = string.Empty;
    public string DurationLabel { get; set; } = string.Empty;
    public string? CourseColor { get; set; }
}
