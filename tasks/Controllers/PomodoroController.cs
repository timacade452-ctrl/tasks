using Microsoft.AspNetCore.Mvc;
using StudyFlow.Services;
using StudyFlow.ViewModels;

namespace StudyFlow.Controllers;

public class PomodoroController : Controller
{
    private readonly IPomodoroService _pomodoro;
    private readonly IUserProfileService _profiles;
    private readonly ICourseService _courses;
    private readonly ITaskService _tasks;
    private readonly IAnalyticsService _analytics;

    public PomodoroController(
        IPomodoroService pomodoro,
        IUserProfileService profiles,
        ICourseService courses,
        ITaskService tasks,
        IAnalyticsService analytics)
    {
        _pomodoro = pomodoro;
        _profiles = profiles;
        _courses = courses;
        _tasks = tasks;
        _analytics = analytics;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Pomodoro Timer";
        ViewData["Subtitle"] = "Focus in short, deliberate sessions — then take a real break.";

        return View(new PomodoroViewModel
        {
            Profile = await _profiles.GetAsync(),
            Courses = await _courses.GetAllAsync(),
            ActiveTasks = await _tasks.GetActiveAsync(),
            RecentSessions = await _pomodoro.GetRecentAsync(8),
            Summary = await _analytics.GetSummaryAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record([FromBody] RecordPomodoroRequest request)
    {
        try
        {
            if (request is null)
            {
                return BadRequest(new { error = "No session data was received." });
            }

            var result = await _pomodoro.RecordAsync(request);
            return Json(new
            {
                ok = true,
                todayFocusMinutes = result.TodayFocusMinutes,
                todaySessions = result.TodaySessions,
                totalFocusMinutes = result.TotalFocusMinutes,
                streak = result.Streak,
                courseName = result.CourseName,
                courseColor = result.CourseColor,
                taskTitle = result.TaskTitle,
                completedAtLabel = result.CompletedAtLabel,
                durationLabel = result.DurationLabel
            });
        }
        catch (StudyFlowValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
