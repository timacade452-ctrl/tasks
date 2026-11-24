using Microsoft.AspNetCore.Mvc;
using StudyFlow.Services;

namespace StudyFlow.Controllers;

public class CalendarController : Controller
{
    private readonly IPlannerService _planner;

    public CalendarController(IPlannerService planner) => _planner = planner;

    public async Task<IActionResult> Index(string? view, DateTime? date)
    {
        ViewData["Title"] = "Calendar";
        ViewData["Subtitle"] = "Deadlines, exams and study blocks across your semester.";

        var model = await _planner.GetCalendarAsync(view ?? "month", date ?? DateTime.Today);
        return View(model);
    }
}
