using StudyFlow.Models;

namespace StudyFlow.ViewModels;

public class CalendarDayViewModel
{
    public DateTime Date { get; init; }
    public bool IsCurrentMonth { get; init; } = true;
    public bool IsToday { get; init; }
    public IReadOnlyList<PlanListItemViewModel> Plans { get; init; } = [];
    public IReadOnlyList<TaskListItemViewModel> Deadlines { get; init; } = [];
    public int TotalItems => Plans.Count + Deadlines.Count;
}

public class CalendarViewModel
{
    /// <summary>month, week or day</summary>
    public string View { get; set; } = "month";
    public DateTime Anchor { get; set; } = DateTime.Today;
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<CalendarDayViewModel> Days { get; set; } = [];
    public CalendarDayViewModel SelectedDay { get; set; } = new() { Date = DateTime.Today };
    public IReadOnlyList<CalendarDayViewModel> WeekDays { get; set; } = [];
    public DateTime PreviousAnchor { get; set; }
    public DateTime NextAnchor { get; set; }
    public int MonthPlanCount { get; set; }
    public int MonthDeadlineCount { get; set; }
}
