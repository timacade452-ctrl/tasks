using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StudyFlow.Helpers;
using StudyFlow.ViewModels;

namespace StudyFlow.Services;

/// <summary>Supplies profile and notification data used by the shared layout.</summary>
public class LayoutDataFilter : IAsyncActionFilter
{
    private readonly IUserProfileService _profiles;
    private readonly ITaskService _tasks;
    private readonly ILogger<LayoutDataFilter> _logger;

    public LayoutDataFilter(IUserProfileService profiles, ITaskService tasks, ILogger<LayoutDataFilter> logger)
    {
        _profiles = profiles;
        _tasks = tasks;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is Controller controller)
        {
            try
            {
                var profile = await _profiles.GetAsync();
                var upcoming = await _tasks.GetUpcomingAsync(5);
                var alerts = new List<NotificationItem>();

                var overdue = upcoming.Where(t => t.Task.IsOverdue).ToList();
                if (overdue.Count > 0)
                {
                    alerts.Add(new NotificationItem
                    {
                        Title = overdue.Count == 1 ? "1 task is overdue" : $"{overdue.Count} tasks are overdue",
                        Detail = overdue[0].Task.Title,
                        Icon = "alert-circle",
                        Tone = "rose",
                        Url = "/Tasks?OverdueOnly=true"
                    });
                }

                foreach (var item in upcoming
                             .Where(t => !t.Task.IsOverdue && (t.Task.DueDate - DateTime.Today).Days <= 2)
                             .Take(3))
                {
                    alerts.Add(new NotificationItem
                    {
                        Title = DisplayHelpers.DueLabel(item.Task.DueDate),
                        Detail = item.Task.Title,
                        Icon = "calendar-clock",
                        Tone = "amber",
                        Url = $"/Tasks/Details/{item.Task.Id}"
                    });
                }

                controller.ViewData["Profile"] = profile;
                controller.ViewData["Alerts"] = alerts;
                controller.ViewData["TodayLabel"] = DateTime.Today.ToString("dddd, MMMM d");
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not prepare layout data.");
                controller.ViewData["Profile"] = new Models.UserProfile();
                controller.ViewData["Alerts"] = Array.Empty<NotificationItem>();
            }
        }

        await next();
    }
}
