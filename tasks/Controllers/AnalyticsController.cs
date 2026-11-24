using Microsoft.AspNetCore.Mvc;
using StudyFlow.Services;

namespace StudyFlow.Controllers;

public class AnalyticsController : Controller
{
    private readonly IAnalyticsService _analytics;

    public AnalyticsController(IAnalyticsService analytics) => _analytics = analytics;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Analytics";
        ViewData["Subtitle"] = "An honest look at where your study time is going.";
        return View(await _analytics.GetAnalyticsAsync());
    }
}
