using StudyFlow.Models;

namespace StudyFlow.ViewModels;

public class DailyStudyPoint
{
    public DateTime Date { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Minutes { get; init; }
    public int CompletedTasks { get; init; }
    public int Sessions { get; init; }
}

public class ActivityItem
{
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string Icon { get; init; } = "activity";
    public string Tone { get; init; } = "indigo";
    public DateTime When { get; init; }
    public string? Url { get; init; }
}

public class NotificationItem
{
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string Icon { get; init; } = "bell";
    public string Tone { get; init; } = "amber";
    public string Url { get; init; } = "#";
}

public class TaskListItemViewModel
{
    public AcademicTask Task { get; init; } = new();
    public Course? Course { get; init; }
}

public class PlanListItemViewModel
{
    public StudyPlanEvent Plan { get; init; } = new();
    public Course? Course { get; init; }
}

public class CourseSummaryViewModel
{
    public Course Course { get; init; } = new();
    public int MaterialsCount { get; init; }
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int FocusMinutes { get; init; }
    public int OpenTasks => Math.Max(0, TotalTasks - CompletedTasks);
    public int TaskProgress => Helpers.DisplayHelpers.Percent(CompletedTasks, TotalTasks);
}

public class StudySummary
{
    public int TotalFocusMinutes { get; init; }
    public int TotalSessions { get; init; }
    public int TodayFocusMinutes { get; init; }
    public int TodaySessions { get; init; }
    public int CompletedTasks { get; init; }
    public int PendingTasks { get; init; }
    public int OverdueTasks { get; init; }
    public int Streak { get; init; }
    public int DailyGoalMinutes { get; init; }
}

public class ProgressRingViewModel
{
    public int Percent { get; init; }
    public string PrimaryText { get; init; } = "0%";
    public string SecondaryText { get; init; } = string.Empty;
    public int Size { get; init; } = 152;
    public string Color { get; init; } = "#4f46e5";
}

public class StudyBarsViewModel
{
    public IReadOnlyList<DailyStudyPoint> Points { get; init; } = [];
    public int MaxMinutes { get; init; } = 60;
    public bool ShowValues { get; init; } = true;
    public bool Compact { get; init; }
}

public class EmptyStateViewModel
{
    public string Icon { get; init; } = "inbox";
    public string Title { get; init; } = "Nothing here yet";
    public string Text { get; init; } = "New items will appear here.";
    public string? ActionUrl { get; init; }
    public string? ActionLabel { get; init; }
}

public class StatCardViewModel
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = "0";
    public string Caption { get; init; } = string.Empty;
    public string Icon { get; init; } = "activity";
    public string Tone { get; init; } = "brand";
}

