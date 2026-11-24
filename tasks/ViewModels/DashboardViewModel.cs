using StudyFlow.Models;

namespace StudyFlow.ViewModels;

public class DashboardViewModel
{
    public UserProfile Profile { get; set; } = new();
    public IReadOnlyList<CourseSummaryViewModel> Courses { get; set; } = [];
    public IReadOnlyList<DailyStudyPoint> Last7Days { get; set; } = [];
    public IReadOnlyList<TaskListItemViewModel> UpcomingDeadlines { get; set; } = [];
    public IReadOnlyList<PlanListItemViewModel> TodaySchedule { get; set; } = [];
    public IReadOnlyList<ActivityItem> RecentActivity { get; set; } = [];

    public int TotalCourses { get; set; }
    public int TotalMaterials { get; set; }
    public int PendingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int TotalFocusMinutes { get; set; }
    public int TodayFocusMinutes { get; set; }
    public int TodaySessions { get; set; }
    public int TodayCompletedTasks { get; set; }
    public int DailyGoalMinutes { get; set; }
    public int DailyGoalPercent { get; set; }
    public int Streak { get; set; }
    public int WeekFocusMinutes { get; set; }
    public int PreviousWeekFocusMinutes { get; set; }

    public int WeekChangePercent
    {
        get
        {
            if (PreviousWeekFocusMinutes <= 0) return WeekFocusMinutes > 0 ? 100 : 0;
            return (int)Math.Round((WeekFocusMinutes - PreviousWeekFocusMinutes) * 100d / PreviousWeekFocusMinutes);
        }
    }

    public int MaxWeeklyMinutes =>
        Last7Days.Count == 0 ? 60 : Math.Max(60, Last7Days.Max(d => d.Minutes));
}
