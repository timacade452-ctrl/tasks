using StudyFlow.Models;

namespace StudyFlow.ViewModels;

public class CourseStudyStat
{
    public Course Course { get; init; } = new();
    public int FocusMinutes { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int Materials { get; init; }
    public int Progress => Helpers.DisplayHelpers.Percent(CompletedTasks, TotalTasks);
}

public class TaskTypeStat
{
    public AcademicTaskType Type { get; init; }
    public int Total { get; init; }
    public int Completed { get; init; }
}

public class AnalyticsViewModel
{
    public UserProfile Profile { get; set; } = new();
    public StudySummary Summary { get; set; } = new();
    public IReadOnlyList<DailyStudyPoint> Last7Days { get; set; } = [];
    public IReadOnlyList<DailyStudyPoint> Last30Days { get; set; } = [];
    public IReadOnlyList<CourseStudyStat> CourseStats { get; set; } = [];
    public IReadOnlyList<TaskTypeStat> TaskTypes { get; set; } = [];
    public IReadOnlyList<ActivityItem> RecentActivity { get; set; } = [];
    public int AverageDailyMinutes { get; set; }
    public int BestDayMinutes { get; set; }
    public string BestDayLabel { get; set; } = "—";
    public int WeekFocusMinutes { get; set; }
    public int PreviousWeekFocusMinutes { get; set; }
    public int WeekChangePercent { get; set; }
    public int MaxWeeklyMinutes { get; set; } = 60;
    public int MaxMonthlyMinutes { get; set; } = 60;
    public int MostProductiveHour { get; set; }
    public string MostProductiveHourLabel { get; set; } = "—";
}
