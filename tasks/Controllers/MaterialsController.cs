using Microsoft.AspNetCore.Mvc;
using StudyFlow.Helpers;
using StudyFlow.Services;
using StudyFlow.ViewModels;

namespace StudyFlow.Controllers;

public class MaterialsController : Controller
{
    private readonly IMaterialService _materials;
    private readonly ICourseService _courses;
    private readonly IFileStorageService _storage;
    private readonly ILogger<MaterialsController> _logger;

    public MaterialsController(
        IMaterialService materials,
        ICourseService courses,
        IFileStorageService storage,
        ILogger<MaterialsController> logger)
    {
        _materials = materials;
        _courses = courses;
        _storage = storage;
        _logger = logger;
    }

    public async Task<IActionResult> Index([FromQuery] MaterialFilterViewModel filter)
    {
        ViewData["Title"] = "Study Materials";
        ViewData["Subtitle"] = "Lecture notes, slides and briefs, organized by course.";
        return View(await _materials.GetListAsync(filter));
    }

    public async Task<IActionResult> Upload()
    {
        ViewData["Title"] = "Upload Material";
        ViewData["Subtitle"] = "Add a PDF, Word document or PowerPoint deck to your library.";

        return View(new UploadMaterialViewModel
        {
            Courses = await _courses.GetAllAsync(),
            MaxFileSizeBytes = _storage.MaxFileSizeBytes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Upload(UploadMaterialViewModel form)
    {
        ViewData["Title"] = "Upload Material";
        ViewData["Subtitle"] = "Add a PDF, Word document or PowerPoint deck to your library.";
        form.Courses = await _courses.GetAllAsync();
        form.MaxFileSizeBytes = _storage.MaxFileSizeBytes;

        if (!ModelState.IsValid) return View(form);

        try
        {
            var material = await _materials.UploadAsync(form);
            TempData["Success"] = $"“{material.Title}” was uploaded successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (StudyFlowValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(form);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Material upload failed.");
            ModelState.AddModelError(string.Empty, "The upload could not be completed. Please try again.");
            return View(form);
        }
    }

    public async Task<IActionResult> Preview(string id)
    {
        var model = await _materials.GetPreviewAsync(id);
        if (model is null) return NotFound();

        ViewData["Title"] = model.Material.Title;
        ViewData["Subtitle"] = model.Course is null
            ? "Document preview"
            : $"{model.Course.Code} · {model.Material.Category.DisplayName()}";

        return View(model);
    }

    /// <summary>Streams a stored file for inline preview. Only the stored name from the database is used.</summary>
    [HttpGet]
    public async Task<IActionResult> File(string id)
    {
        var material = await _materials.GetAsync(id);
        if (material is null) return NotFound();

        try
        {
            var stream = _storage.OpenRead(material.StoredFileName);
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(stream, material.ContentType, enableRangeProcessing: true);
        }
        catch (Exception exception) when (exception is FileNotFoundException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "Stored file missing for material {MaterialId}", id);
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Download(string id)
    {
        var material = await _materials.GetAsync(id);
        if (material is null) return NotFound();

        try
        {
            var stream = _storage.OpenRead(material.StoredFileName);
            var downloadName = Services.FileTypes.DownloadName(material.OriginalFileName, material.FileExtension);
            return File(stream, "application/octet-stream", downloadName);
        }
        catch (Exception exception) when (exception is FileNotFoundException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "Stored file missing for material {MaterialId}", id);
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, string? returnUrl = null)
    {
        try
        {
            var material = await _materials.GetAsync(id);
            await _materials.DeleteAsync(id);
            TempData["Success"] = material is null
                ? "Material deleted."
                : $"“{material.Title}” was deleted.";
        }
        catch (StudyFlowValidationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return Url.IsLocalUrl(returnUrl) && !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
