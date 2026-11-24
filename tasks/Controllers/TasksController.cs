using Microsoft.AspNetCore.Mvc;
using StudyFlow.Helpers;
using StudyFlow.Models;
using StudyFlow.Services;
using StudyFlow.ViewModels;

namespace StudyFlow.Controllers;

public class TasksController : Controller
{
    private readonly ITaskService _tasks;
    private readonly ICourseService _courses;

    public TasksController(ITaskService tasks, ICourseService courses)
    {
        _tasks = tasks;
        _courses = courses;
    }

    public async Task<IActionResult> Index([FromQuery] TaskFilterViewModel filter)
    {
        ViewData["Title"] = "All Tasks";
        ViewData["Subtitle"] = "Assignments, revision and reading — everything you need to finish.";
        return View(await _tasks.GetListAsync(filter));
    }

    public async Task<IActionResult> Details(string id)
    {
        var model = await _tasks.GetDetailsAsync(id);
        if (model is null) return NotFound();

        ViewData["Title"] = "Task Details";
        ViewData["Subtitle"] = model.Task.Title;
        return View(model);
    }

    public async Task<IActionResult> Create(string? courseId = null)
    {
        var courses = await _courses.GetAllAsync();
        ViewData["Title"] = "Create Task";
        ViewData["Subtitle"] = "Plan the work, set a deadline and link it to a course.";

        return View(new TaskFormViewModel
        {
            Courses = courses,
            CourseId = courses.Any(c => c.Id == courseId) ? courseId : null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskFormViewModel form)
    {
        form.Courses = await _courses.GetAllAsync();
        ViewData["Title"] = "Create Task";
        ViewData["Subtitle"] = "Plan the work, set a deadline and link it to a course.";

        if (!ModelState.IsValid) return View(form);

        try
        {
            var task = await _tasks.CreateAsync(form);
            TempData["Success"] = $"“{task.Title}” was created.";

            if (Url.IsLocalUrl(form.ReturnUrl) && !string.IsNullOrWhiteSpace(form.ReturnUrl))
            {
                return Redirect(form.ReturnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (StudyFlowValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(form);
        }
    }

    public async Task<IActionResult> Edit(string id)
    {
        var form = await _tasks.GetFormAsync(id);
        if (form is null) return NotFound();

        ViewData["Title"] = "Edit Task";
        ViewData["Subtitle"] = form.Title;
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, TaskFormViewModel form)
    {
        form.Courses = await _courses.GetAllAsync();
        ViewData["Title"] = "Edit Task";
        ViewData["Subtitle"] = form.Title;

        if (form.Id != id) ModelState.AddModelError(string.Empty, "The task reference is invalid.");
        if (!ModelState.IsValid) return View(form);

        try
        {
            await _tasks.UpdateAsync(id, form);
            TempData["Success"] = "Task updated.";
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
    public async Task<IActionResult> Delete(string id, string? returnUrl = null)
    {
        try
        {
            var task = await _tasks.GetAsync(id);
            await _tasks.DeleteAsync(id);
            TempData["Success"] = task is null ? "Task deleted." : $"“{task.Title}” was deleted.";
        }
        catch (StudyFlowValidationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return Url.IsLocalUrl(returnUrl) && !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleComplete(string id, string? returnUrl = null)
    {
        try
        {
            var status = await _tasks.ToggleCompleteAsync(id);
            TempData["Success"] = status == AcademicTaskStatus.Completed
                ? "Nice work — task marked as complete."
                : "Task moved back to your to-do list.";
        }
        catch (StudyFlowValidationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        if (Url.IsLocalUrl(returnUrl) && !string.IsNullOrWhiteSpace(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string id, AcademicTaskStatus status, string? returnUrl = null)
    {
        try
        {
            await _tasks.UpdateStatusAsync(id, status);
            TempData["Success"] = $"Task status changed to {status.DisplayName()}.";
        }
        catch (StudyFlowValidationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        if (Url.IsLocalUrl(returnUrl) && !string.IsNullOrWhiteSpace(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
