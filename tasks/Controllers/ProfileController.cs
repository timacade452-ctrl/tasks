using Microsoft.AspNetCore.Mvc;
using StudyFlow.Services;
using StudyFlow.ViewModels;

namespace StudyFlow.Controllers;

public class ProfileController : Controller
{
    private readonly IUserProfileService _profiles;
    private readonly IAnalyticsService _analytics;
    private readonly ICourseService _courses;
    private readonly IMaterialService _materials;

    public ProfileController(
        IUserProfileService profiles,
        IAnalyticsService analytics,
        ICourseService courses,
        IMaterialService materials)
    {
        _profiles = profiles;
        _analytics = analytics;
        _courses = courses;
        _materials = materials;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Profile";
        ViewData["Subtitle"] = "Your study identity, goals and progress.";
        return View(await BuildAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileFormViewModel form)
    {
        ViewData["Title"] = "Profile";
        ViewData["Subtitle"] = "Your study identity, goals and progress.";

        if (!ModelState.IsValid)
        {
            var model = await BuildAsync();
            model.Form = form;
            return View("Index", model);
        }

        await _profiles.UpdateProfileAsync(form);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfileViewModel> BuildAsync()
    {
        var profile = await _profiles.GetAsync();
        var summaries = await _courses.GetSummariesAsync();
        var materials = await _materials.GetListAsync(new MaterialFilterViewModel());
        var summary = await _analytics.GetSummaryAsync();

        return new ProfileViewModel
        {
            Profile = profile,
            Form = ProfileFormViewModel.FromProfile(profile),
            Summary = summary,
            Courses = summaries,
            RecentActivity = await _analytics.GetRecentActivityAsync(6),
            MaterialsCount = materials.Materials.Count,
            CompletedTasks = summary.CompletedTasks
        };
    }
}
