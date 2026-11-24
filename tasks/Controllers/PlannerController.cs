using Microsoft.AspNetCore.Mvc;
using StudyFlow.Services;
using StudyFlow.ViewModels;

namespace StudyFlow.Controllers;

public class PlannerController : Controller
{
    private readonly IPlannerService _planner;

    public PlannerController(IPlannerService planner) => _planner = planner;

    public async Task<IActionResult> Index(DateTime? date, string? edit = null)
    {
        var day = (date ?? DateTime.Today).Date;
        ViewData["Title"] = "Study Planner";
        ViewData["Subtitle"] = "Design a realistic day, block by block.";

        return View(await _planner.GetPlannerAsync(day, edit));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlanFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Study Planner";
            ViewData["Subtitle"] = "Design a realistic day, block by block.";
            var model = await _planner.GetPlannerAsync(form.Date);
            model.Form = form;
            return View("Index", model);
        }

        try
        {
            await _planner.CreateAsync(form);
            TempData["Success"] = "Study plan added to your calendar.";
            return RedirectToAction(nameof(Index), new { date = form.Date.ToString("yyyy-MM-dd") });
        }
        catch (StudyFlowValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            ViewData["Title"] = "Study Planner";
            ViewData["Subtitle"] = "Design a realistic day, block by block.";
            var model = await _planner.GetPlannerAsync(form.Date);
            model.Form = form;
            return View("Index", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string id, PlanFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Study Planner";
            ViewData["Subtitle"] = "Design a realistic day, block by block.";
            var model = await _planner.GetPlannerAsync(form.Date, id);
            model.Form = form;
            return View("Index", model);
        }

        try
        {
            await _planner.UpdateAsync(id, form);
            TempData["Success"] = "Study plan updated.";
            return RedirectToAction(nameof(Index), new { date = form.Date.ToString("yyyy-MM-dd") });
        }
        catch (StudyFlowValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            ViewData["Title"] = "Study Planner";
            ViewData["Subtitle"] = "Design a realistic day, block by block.";
            var model = await _planner.GetPlannerAsync(form.Date, id);
            model.Form = form;
            return View("Index", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, DateTime? date)
    {
        try
        {
            await _planner.DeleteAsync(id);
            TempData["Success"] = "Study plan removed.";
        }
        catch (StudyFlowValidationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { date = (date ?? DateTime.Today).ToString("yyyy-MM-dd") });
    }
}
