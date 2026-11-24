using Microsoft.AspNetCore.Mvc;
using StudyFlow.Services;

namespace StudyFlow.Controllers;

public class DashboardController : Controller
{
    private readonly IAnalyticsService _analytics;

    public DashboardController(IAnalyticsService analytics) => _analytics = analytics;

    public async Task<IActionResult> Index()
    {
        var model = await _analytics.GetDashboardAsync();
        return View(model);
    }
}
