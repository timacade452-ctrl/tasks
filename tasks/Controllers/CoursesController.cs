using Microsoft.AspNetCore.Mvc;
using StudyFlow.Helpers;
using StudyFlow.Models;
using StudyFlow.Services;
using StudyFlow.ViewModels;

namespace StudyFlow.Controllers;

public class CoursesController : Controller
{
    private readonly ICourseService _courses;

    public CoursesController(ICourseService courses) => _courses = courses;

    public async Task<IActionResult> Index()
    {
        var summaries = await _courses.GetSummariesAsync();

        ViewData["Title"] = "My Courses";
        ViewData["Subtitle"] = "Every course, material and deadline in one place.";

        return View(new CoursesIndexViewModel
        {
            Courses = summaries,
            TotalMaterials = summaries.Sum(c => c.MaterialsCount),
            OpenTasks = summaries.Sum(c => c.OpenTasks),
            FocusMinutes = summaries.Sum(c => c.FocusMinutes),
            CompletedTasks = summaries.Sum(c => c.CompletedTasks)
        });
    }

    public async Task<IActionResult> Details(string id)
    {
        var model = await _courses.GetDetailsAsync(id);
        if (model is null) return NotFound();

        ViewData["Title"] = model.Course.Code;
        ViewData["Subtitle"] = model.Course.Name;

        return View(model);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "Create Course";
        ViewData["Subtitle"] = "Add a course to group your materials, tasks and study sessions.";
        return View(new CourseFormViewModel { Semester = CurrentSemester() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel form)
    {
        ViewData["Title"] = "Create Course";
        ViewData["Subtitle"] = "Add a course to group your materials, tasks and study sessions.";

        if (!ModelState.IsValid) return View(form);

        try
        {
            var course = await _courses.CreateAsync(form);
            TempData["Success"] = $"{course.Code} was added to your courses.";
            return RedirectToAction(nameof(Details), new { id = course.Id });
        }
        catch (StudyFlowValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(form);
        }
    }

    public async Task<IActionResult> Edit(string id)
    {
        var form = await _courses.GetFormAsync(id);
        if (form is null) return NotFound();

        var course = await _courses.GetAsync(id);
        ViewData["Title"] = "Edit Course";
        ViewData["Subtitle"] = course is null ? "Update course details" : $"Update {course.Code} — {course.Name}";
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, CourseFormViewModel form)
    {
        var course = await _courses.GetAsync(id);
        if (course is null) return NotFound();

        ViewData["Title"] = "Edit Course";
        ViewData["Subtitle"] = $"Update {course.Code} — {course.Name}";

        if (form.Id != id) ModelState.AddModelError(string.Empty, "The course reference is invalid.");
        if (!ModelState.IsValid) return View(form);

        try
        {
            await _courses.UpdateAsync(id, form);
            TempData["Success"] = "Course details were updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (StudyFlowValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(form);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var course = await _courses.GetAsync(id);
            await _courses.DeleteAsync(id);
            TempData["Success"] = course is null
                ? "Course deleted."
                : $"{course.Code} and its related study records were deleted.";
        }
        catch (StudyFlowValidationException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        return RedirectToAction(nameof(Index));
    }

    private static string CurrentSemester()
    {
        var today = DateTime.Today;
        var season = today.Month switch
        {
            >= 8 => "Fall",
            >= 5 => "Summer",
            >= 2 => "Spring",
            _ => "Winter"
        };
        return $"{season} {today.Year}";
    }
}
