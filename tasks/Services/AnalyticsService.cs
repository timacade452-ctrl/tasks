using System.Globalization;
using StudyFlow.Helpers;
using StudyFlow.Models;
using StudyFlow.Repositories;
using StudyFlow.ViewModels;

namespace StudyFlow.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ICourseRepository _courses;
    private readonly ITaskRepository _tasks;
    private readonly IMaterialRepository _materials;
    private readonly IPomodoroRepository _sessions;
    private readonly IStudyPlanRepository _plans;
    private readonly IUserProfileRepository _profiles;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsService(
        ICourseRepository courses,
        ITaskRepository tasks,
        IMaterialRepository materials,
        IPomodoroRepository sessions,
        IStudyPlanRepository plans,
        IUserProfileRepository profiles,
        ICurrentUserService currentUser)
    {
        _courses = courses;
        _tasks = tasks;
        _materials = materials;
        _sessions = sessions;
        _plans = plans;
        _profiles = profiles;
        _currentUser = currentUser;
    }

    public async Task<StudySummary> GetSummaryAsync()
    {
        var data = await LoadAsync();
        return BuildSummary(data);
    }

    public async Task<int> GetStreakAsync()
    {
        var sessions = await _sessions.GetAllAsync(_currentUser.UserId);
        return PomodoroService.CalculateStreak(sessions);
    }

    public async Task<IReadOnlyList<DailyStudyPoint>> GetDailySeriesAsync(int days)
    {
        var sessions = await _sessions.GetAllAsync(_currentUser.UserId);
        var tasks = await _tasks.GetAllAsync(_currentUser.UserId);
        return BuildSeries(sessions, tasks, days);
    }

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var data = await LoadAsync();
        var summary = BuildSummary(data);
        var today = DateTime.Today;

        var courseLookup = data.Courses.ToDictionary(c => c.Id);
        var upcoming = data.Tasks
            .Where(t => t.Status != AcademicTaskStatus.Completed)
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.Priority)
            .Take(5)
            .Select(t => new TaskListItemViewModel
            {
                Task = t,
                Course = t.CourseId is not null ? courseLookup.GetValueOrDefault(t.CourseId) : null
            })
            .ToList();

        var todayPlans = data.Plans
            .Where(p => p.Start < today.AddDays(1) && p.End > today)
            .OrderBy(p => p.Start)
            .Select(p => new PlanListItemViewModel
            {
                Plan = p,
                Course = p.CourseId is not null ? courseLookup.GetValueOrDefault(p.CourseId) : null
            })
            .ToList();

        var weekSeries = BuildSeries(data.Sessions, data.Tasks, 7);
        var weekFocus = weekSeries.Sum(p => p.Minutes);
        var previousStart = today.AddDays(-13);
        var previousEnd = today.AddDays(-6);
        var previousWeekFocus = data.Sessions
            .Where(s => s.Phase == PomodoroPhase.Focus &&
                        s.CompletedAt.Date >= previousStart &&
                        s.CompletedAt.Date <= previousEnd)
            .Sum(s => s.DurationMinutes);

        return new DashboardViewModel
        {
            Profile = data.Profile,
            Courses = BuildCourseSummaries(data),
            Last7Days = weekSeries,
            UpcomingDeadlines = upcoming,
            TodaySchedule = todayPlans,
            RecentActivity = BuildActivity(data, 6),
            TotalCourses = data.Courses.Count,
            TotalMaterials = data.Materials.Count,
            PendingTasks = data.Tasks.Count(t => t.Status != AcademicTaskStatus.Completed),
            CompletedTasks = data.Tasks.Count(t => t.Status == AcademicTaskStatus.Completed),
            OverdueTasks = data.Tasks.Count(t => t.IsOverdue),
            TotalFocusMinutes = summary.TotalFocusMinutes,
            TodayFocusMinutes = summary.TodayFocusMinutes,
            TodaySessions = summary.TodaySessions,
            TodayCompletedTasks = data.Tasks.Count(t => t.CompletedAt?.Date == today),
            DailyGoalMinutes = data.Profile.DailyGoalMinutes,
            DailyGoalPercent = DisplayHelpers.Percent(summary.TodayFocusMinutes, data.Profile.DailyGoalMinutes),
            Streak = summary.Streak,
            WeekFocusMinutes = weekFocus,
            PreviousWeekFocusMinutes = previousWeekFocus
        };
    }

    public async Task<AnalyticsViewModel> GetAnalyticsAsync()
    {
        var data = await LoadAsync();
        var summary = BuildSummary(data);
        var today = DateTime.Today;

        var last7 = BuildSeries(data.Sessions, data.Tasks, 7);
        var last30 = BuildSeries(data.Sessions, data.Tasks, 30);
        var weekFocus = last7.Sum(d => d.Minutes);

        var previousWeekFocus = data.Sessions
            .Where(s => s.Phase == PomodoroPhase.Focus &&
                        s.CompletedAt.Date >= today.AddDays(-13) &&
                        s.CompletedAt.Date <= today.AddDays(-6))
            .Sum(s => s.DurationMinutes);

        var best = last30.OrderByDescending(d => d.Minutes).FirstOrDefault();
        var twentyNineDaysAgo = today.AddDays(-29);
        var recentSessions = data.Sessions
            .Where(s => s.Phase == PomodoroPhase.Focus && s.CompletedAt.Date >= twentyNineDaysAgo)
            .ToList();

        var productiveHour = recentSessions
            .GroupBy(s => s.CompletedAt.Hour)
            .OrderByDescending(g => g.Sum(s => s.DurationMinutes))
            .FirstOrDefault();

        var taskTypes = Enum.GetValues<AcademicTaskType>()
            .Select(type => new TaskTypeStat
            {
                Type = type,
                Total = data.Tasks.Count(t => t.Type == type),
                Completed = data.Tasks.Count(t => t.Type == type && t.Status == AcademicTaskStatus.Completed)
            })
            .Where(stat => stat.Total > 0)
            .ToList();

        return new AnalyticsViewModel
        {
            Profile = data.Profile,
            Summary = summary,
            Last7Days = last7,
            Last30Days = last30,
            CourseStats = BuildCourseSummaries(data)
                .Select(c => new CourseStudyStat
                {
                    Course = c.Course,
                    FocusMinutes = c.FocusMinutes,
                    TotalTasks = c.TotalTasks,
                    CompletedTasks = c.CompletedTasks,
                    Materials = c.MaterialsCount
                })
                .OrderByDescending(c => c.FocusMinutes)
                .ToList(),
            TaskTypes = taskTypes,
            RecentActivity = BuildActivity(data, 8),
            AverageDailyMinutes = (int)Math.Round(last30.Sum(d => d.Minutes) / 30d),
            BestDayMinutes = best?.Minutes ?? 0,
            BestDayLabel = best is null ? "—" : best.Date.ToString("ddd, MMM d", CultureInfo.InvariantCulture),
            WeekFocusMinutes = weekFocus,
            PreviousWeekFocusMinutes = previousWeekFocus,
            WeekChangePercent = ChangePercent(weekFocus, previousWeekFocus),
            MaxWeeklyMinutes = Math.Max(60, last7.Count == 0 ? 60 : last7.Max(d => d.Minutes)),
            MaxMonthlyMinutes = Math.Max(60, last30.Count == 0 ? 60 : last30.Max(d => d.Minutes)),
            MostProductiveHour = productiveHour?.Key ?? 0,
            MostProductiveHourLabel = productiveHour is null ? "—" : HourLabel(productiveHour.Key)
        };
    }

    public async Task<IReadOnlyList<ActivityItem>> GetRecentActivityAsync(int take = 6)
    {
        var data = await LoadAsync();
        return BuildActivity(data, take);
    }

    private async Task<AnalyticsData> LoadAsync()
    {
        var userId = _currentUser.UserId;
        var profile = await _profiles.GetAsync(userId) ?? new UserProfile { Id = userId };
        return new AnalyticsData
        {
            Profile = profile,
            Courses = (await _courses.GetAllAsync(userId)).OrderBy(c => c.Code).ToList(),
            Tasks = (await _tasks.GetAllAsync(userId)).ToList(),
            Materials = (await _materials.GetAllAsync(userId)).ToList(),
            Sessions = (await _sessions.GetAllAsync(userId)).ToList(),
            Plans = (await _plans.GetAllAsync(userId)).ToList()
        };
    }

    private static StudySummary BuildSummary(AnalyticsData data)
    {
        var focusSessions = data.Sessions.Where(s => s.Phase == PomodoroPhase.Focus).ToList();
        var today = DateTime.Today;
        var todaySessions = focusSessions.Where(s => s.CompletedAt.Date == today).ToList();

        return new StudySummary
        {
            TotalFocusMinutes = focusSessions.Sum(s => s.DurationMinutes),
            TotalSessions = focusSessions.Count,
            TodayFocusMinutes = todaySessions.Sum(s => s.DurationMinutes),
            TodaySessions = todaySessions.Count,
            CompletedTasks = data.Tasks.Count(t => t.Status == AcademicTaskStatus.Completed),
            PendingTasks = data.Tasks.Count(t => t.Status != AcademicTaskStatus.Completed),
            OverdueTasks = data.Tasks.Count(t => t.IsOverdue),
            Streak = PomodoroService.CalculateStreak(focusSessions),
            DailyGoalMinutes = data.Profile.DailyGoalMinutes
        };
    }

    private static List<DailyStudyPoint> BuildSeries(
        IEnumerable<PomodoroSession> sessions,
        IEnumerable<AcademicTask> tasks,
        int days)
    {
        var today = DateTime.Today;
        var start = today.AddDays(-(days - 1));
        var sessionList = sessions.Where(s => s.Phase == PomodoroPhase.Focus).ToList();
        var taskList = tasks.ToList();

        return Enumerable.Range(0, days)
            .Select(offset =>
            {
                var date = start.AddDays(offset);
                var daySessions = sessionList.Where(s => s.CompletedAt.Date == date).ToList();
                return new DailyStudyPoint
                {
                    Date = date,
                    Label = date.ToString(days <= 7 ? "ddd" : "MMM d", CultureInfo.InvariantCulture),
                    Minutes = daySessions.Sum(s => s.DurationMinutes),
                    Sessions = daySessions.Count,
                    CompletedTasks = taskList.Count(t => t.CompletedAt?.Date == date)
                };
            })
            .ToList();
    }

    private static List<CourseSummaryViewModel> BuildCourseSummaries(AnalyticsData data) =>
        data.Courses.Select(c => new CourseSummaryViewModel
        {
            Course = c,
            MaterialsCount = data.Materials.Count(m => m.CourseId == c.Id),
            TotalTasks = data.Tasks.Count(t => t.CourseId == c.Id),
            CompletedTasks = data.Tasks.Count(t => t.CourseId == c.Id && t.Status == AcademicTaskStatus.Completed),
            FocusMinutes = data.Sessions
                .Where(s => s.CourseId == c.Id && s.Phase == PomodoroPhase.Focus)
                .Sum(s => s.DurationMinutes)
        }).ToList();

    private static List<ActivityItem> BuildActivity(AnalyticsData data, int take)
    {
        var courseLookup = data.Courses.ToDictionary(c => c.Id);
        var taskLookup = data.Tasks.ToDictionary(t => t.Id);
        var activity = new List<ActivityItem>();

        foreach (var session in data.Sessions.Where(s => s.Phase == PomodoroPhase.Focus))
        {
            var course = session.CourseId is not null ? courseLookup.GetValueOrDefault(session.CourseId) : null;
            var taskTitle = session.TaskId is not null && taskLookup.TryGetValue(session.TaskId, out var task)
                ? task.Title
                : null;
            var detail = course?.Code ?? "General study";
            if (!string.IsNullOrWhiteSpace(taskTitle))
            {
                detail += $" · {taskTitle}";
            }

            activity.Add(new ActivityItem
            {
                Title = $"Focus session · {session.DurationMinutes} min",
                Detail = detail,
                Icon = "timer",
                Tone = "indigo",
                When = session.CompletedAt,
                Url = "/Pomodoro"
            });
        }

        foreach (var task in data.Tasks.Where(t => t.Status == AcademicTaskStatus.Completed && t.CompletedAt.HasValue))
        {
            var course = task.CourseId is not null ? courseLookup.GetValueOrDefault(task.CourseId) : null;
            activity.Add(new ActivityItem
            {
                Title = "Task completed",
                Detail = course is not null ? $"{task.Title} · {course.Code}" : task.Title,
                Icon = "check-circle-2",
                Tone = "emerald",
                When = task.CompletedAt!.Value,
                Url = $"/Tasks/Details/{task.Id}"
            });
        }

        foreach (var material in data.Materials)
        {
            var course = courseLookup.GetValueOrDefault(material.CourseId);
            activity.Add(new ActivityItem
            {
                Title = "Material uploaded",
                Detail = course is not null ? $"{material.Title} · {course.Code}" : material.Title,
                Icon = "file-text",
                Tone = "sky",
                When = material.UploadedAt,
                Url = $"/Materials/Preview/{material.Id}"
            });
        }

        return activity
            .OrderByDescending(a => a.When)
            .Take(take)
            .ToList();
    }

    private static int ChangePercent(int current, int previous)
    {
        if (previous <= 0) return current > 0 ? 100 : 0;
        return (int)Math.Round((current - previous) * 100d / previous);
    }

    private static string HourLabel(int hour)
    {
        var start = new DateTime(2000, 1, 1, hour, 0, 0);
        var end = start.AddHours(1);
        return $"{start.ToString("h tt", CultureInfo.InvariantCulture)} – {end.ToString("h tt", CultureInfo.InvariantCulture)}";
    }

    private sealed class AnalyticsData
    {
        public UserProfile Profile { get; init; } = new();
        public List<Course> Courses { get; init; } = [];
        public List<AcademicTask> Tasks { get; init; } = [];
        public List<StudyMaterial> Materials { get; init; } = [];
        public List<PomodoroSession> Sessions { get; init; } = [];
        public List<StudyPlanEvent> Plans { get; init; } = [];
    }
}
