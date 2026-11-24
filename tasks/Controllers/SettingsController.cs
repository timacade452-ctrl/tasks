using Microsoft.AspNetCore.Mvc;
using StudyFlow.Services;
using StudyFlow.ViewModels;

namespace StudyFlow.Controllers;

public class SettingsController : Controller
{
    private readonly IUserProfileService _profiles;
    private readonly IAnalyticsService _analytics;

    public SettingsController(IUserProfileService profiles, IAnalyticsService analytics)
    {
        _profiles = profiles;
        _analytics = analytics;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Settings";
        ViewData["Subtitle"] = "Tune your study rhythm: goals, timers and notifications.";

        var profile = await _profiles.GetAsync();
        return View(new SettingsViewModel
        {
            DailyGoalMinutes = profile.DailyGoalMinutes,
            FocusMinutes = profile.FocusMinutes,
            ShortBreakMinutes = profile.ShortBreakMinutes,
            LongBreakMinutes = profile.LongBreakMinutes,
            LongBreakInterval = profile.LongBreakInterval,
            SoundEnabled = profile.SoundEnabled,
            AutoStartBreaks = profile.AutoStartBreaks,
            Profile = profile,
            Summary = await _analytics.GetSummaryAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(SettingsViewModel form)
    {
        ViewData["Title"] = "Settings";
        ViewData["Subtitle"] = "Tune your study rhythm: goals, timers and notifications.";

        if (!ModelState.IsValid)
        {
            var current = await _profiles.GetAsync();
            form.Profile = current;
            form.Summary = await _analytics.GetSummaryAsync();
            return View("Index", form);
        }

        await _profiles.UpdateSettingsAsync(form);
        TempData["Success"] = "Your settings were saved.";
        return RedirectToAction(nameof(Index));
    }
}
