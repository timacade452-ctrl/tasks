using StudyFlow.Models;
using StudyFlow.Repositories;
using StudyFlow.ViewModels;

namespace StudyFlow.Services;

public class PomodoroService : IPomodoroService
{
    private readonly IPomodoroRepository _sessions;
    private readonly ICourseRepository _courses;
    private readonly ITaskRepository _tasks;
    private readonly ICurrentUserService _currentUser;

    public PomodoroService(
        IPomodoroRepository sessions,
        ICourseRepository courses,
        ITaskRepository tasks,
        ICurrentUserService currentUser)
    {
        _sessions = sessions;
        _courses = courses;
        _tasks = tasks;
        _currentUser = currentUser;
    }

    public async Task<PomodoroRecordResult> RecordAsync(RecordPomodoroRequest request)
    {
        if (request.Minutes < 1 || request.Minutes > 180)
        {
            throw new StudyFlowValidationException("Focus sessions must be between 1 and 180 minutes.");
        }

        var userId = _currentUser.UserId;
        var course = string.IsNullOrWhiteSpace(request.CourseId)
            ? null
            : await _courses.GetByIdAsync(userId, request.CourseId);
        var task = string.IsNullOrWhiteSpace(request.TaskId)
            ? null
            : await _tasks.GetByIdAsync(userId, request.TaskId);

        if (task is not null)
        {
            course ??= task.CourseId is not null
                ? await _courses.GetByIdAsync(userId, task.CourseId)
                : null;
        }

        if (!string.IsNullOrWhiteSpace(request.CourseId) && course is null)
        {
            throw new StudyFlowValidationException("The selected course could not be found.");
        }

        var now = DateTime.Now;
        var completedAt = request.CompletedAt?.ToLocalTime() ?? now;
        if (completedAt > now.AddMinutes(2) || completedAt < now.AddHours(-12))
        {
            completedAt = now;
        }

        var startedAt = request.StartedAt?.ToLocalTime() ?? completedAt.AddMinutes(-request.Minutes);
        if (startedAt >= completedAt || startedAt < completedAt.AddHours(-6))
        {
            startedAt = completedAt.AddMinutes(-request.Minutes);
        }

        var session = new PomodoroSession
        {
            UserId = userId,
            CourseId = course?.Id,
            TaskId = task?.Id,
            Phase = PomodoroPhase.Focus,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            DurationMinutes = request.Minutes
        };

        await _sessions.AddAsync(session);

        var all = await _sessions.GetAllAsync(userId);
        var today = DateTime.Today;
        var todaySessions = all.Where(s => s.Phase == PomodoroPhase.Focus && s.CompletedAt.Date == today).ToList();

        return new PomodoroRecordResult
        {
            TodayFocusMinutes = todaySessions.Sum(s => s.DurationMinutes),
            TodaySessions = todaySessions.Count,
            TotalFocusMinutes = all.Where(s => s.Phase == PomodoroPhase.Focus).Sum(s => s.DurationMinutes),
            Streak = CalculateStreak(all),
            CourseName = course?.Name,
            CourseColor = course?.Color,
            TaskTitle = task?.Title,
            CompletedAtLabel = completedAt.ToString("h:mm tt"),
            DurationLabel = $"{session.DurationMinutes} min"
        };
    }

    public async Task<IReadOnlyList<PomodoroSessionViewModel>> GetRecentAsync(int take = 8)
    {
        var userId = _currentUser.UserId;
        var courses = (await _courses.GetAllAsync(userId)).ToDictionary(c => c.Id);
        var tasks = (await _tasks.GetAllAsync(userId)).ToDictionary(t => t.Id);

        return (await _sessions.GetAllAsync(userId))
            .Where(s => s.Phase == PomodoroPhase.Focus)
            .OrderByDescending(s => s.CompletedAt)
            .Take(take)
            .Select(s => new PomodoroSessionViewModel
            {
                Session = s,
                Course = s.CourseId is not null ? courses.GetValueOrDefault(s.CourseId) : null,
                TaskTitle = s.TaskId is not null && tasks.TryGetValue(s.TaskId, out var task) ? task.Title : null
            })
            .ToList();
    }

    internal static int CalculateStreak(IEnumerable<PomodoroSession> sessions)
    {
        var days = sessions
            .Where(s => s.Phase == PomodoroPhase.Focus)
            .Select(s => s.CompletedAt.Date)
            .Distinct()
            .ToHashSet();

        if (days.Count == 0) return 0;

        var cursor = DateTime.Today;
        if (!days.Contains(cursor))
        {
            // A streak is only broken once a full day has passed.
            cursor = cursor.AddDays(-1);
            if (!days.Contains(cursor)) return 0;
        }

        var streak = 0;
        while (days.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }
}
