using StudyFlow.Helpers;
using StudyFlow.Models;
using StudyFlow.Repositories;
using StudyFlow.ViewModels;

namespace StudyFlow.Services;

public class MaterialService : IMaterialService
{
    private readonly IMaterialRepository _materials;
    private readonly ICourseRepository _courses;
    private readonly IFileStorageService _storage;
    private readonly IFileValidationService _validation;
    private readonly ICurrentUserService _currentUser;

    public MaterialService(
        IMaterialRepository materials,
        ICourseRepository courses,
        IFileStorageService storage,
        IFileValidationService validation,
        ICurrentUserService currentUser)
    {
        _materials = materials;
        _courses = courses;
        _storage = storage;
        _validation = validation;
        _currentUser = currentUser;
    }

    public async Task<MaterialListViewModel> GetListAsync(MaterialFilterViewModel filter)
    {
        var userId = _currentUser.UserId;
        var courses = await _courses.GetAllAsync(userId);
        var courseLookup = courses.ToDictionary(c => c.Id);
        var materials = await _materials.GetAllAsync(userId);

        var search = filter.Q?.Trim();
        var filtered = materials.Where(m =>
            (string.IsNullOrWhiteSpace(search) ||
             m.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             (m.Description ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
             m.OriginalFileName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             (courseLookup.TryGetValue(m.CourseId, out var searchCourse) &&
              (searchCourse.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               searchCourse.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))) &&
            (string.IsNullOrWhiteSpace(filter.CourseId) || m.CourseId == filter.CourseId) &&
            FileTypes.MatchesKind(m.FileExtension, filter.Kind) &&
            (!filter.Category.HasValue || m.Category == filter.Category))
            .ToList();

        filtered = filter.Sort switch
        {
            "oldest" => filtered.OrderBy(m => m.UploadedAt).ToList(),
            "title" => filtered.OrderBy(m => m.Title, StringComparer.OrdinalIgnoreCase).ToList(),
            "size" => filtered.OrderByDescending(m => m.FileSizeBytes).ToList(),
            _ => filtered.OrderByDescending(m => m.UploadedAt).ToList()
        };

        return new MaterialListViewModel
        {
            Materials = filtered.Select(m => new MaterialListItemViewModel
            {
                Material = m,
                Course = courseLookup.GetValueOrDefault(m.CourseId)
            }).ToList(),
            Courses = courses.OrderBy(c => c.Code).ToList(),
            Filters = filter,
            PdfCount = materials.Count(m => m.FileExtension == ".pdf"),
            WordCount = materials.Count(m => m.FileExtension is ".doc" or ".docx"),
            SlidesCount = materials.Count(m => m.FileExtension is ".ppt" or ".pptx"),
            TotalSize = materials.Sum(m => m.FileSizeBytes)
        };
    }

    public async Task<StudyMaterial?> GetAsync(string id) =>
        await _materials.GetByIdAsync(_currentUser.UserId, id);

    public async Task<MaterialPreviewViewModel?> GetPreviewAsync(string id)
    {
        var userId = _currentUser.UserId;
        var material = await _materials.GetByIdAsync(userId, id);
        if (material is null) return null;

        var course = await _courses.GetByIdAsync(userId, material.CourseId);
        var related = (await _materials.GetByCourseAsync(userId, material.CourseId))
            .Where(m => m.Id != material.Id)
            .OrderByDescending(m => m.UploadedAt)
            .Take(4)
            .Select(m => new MaterialListItemViewModel { Material = m, Course = course })
            .ToList();

        return new MaterialPreviewViewModel
        {
            Material = material,
            Course = course,
            Related = related
        };
    }

    public async Task<IReadOnlyList<StudyMaterial>> GetByCourseAsync(string courseId)
    {
        var materials = await _materials.GetByCourseAsync(_currentUser.UserId, courseId);
        return materials.OrderByDescending(m => m.UploadedAt).ToList();
    }

    public async Task<StudyMaterial> UploadAsync(UploadMaterialViewModel form)
    {
        var validation = _validation.Validate(form.File);
        if (!validation.IsValid)
        {
            throw new StudyFlowValidationException(validation.Error ?? "The file could not be validated.");
        }

        if (string.IsNullOrWhiteSpace(form.CourseId))
        {
            throw new StudyFlowValidationException("Select a course for this document.");
        }

        var course = await _courses.GetByIdAsync(_currentUser.UserId, form.CourseId);
        if (course is null)
        {
            throw new StudyFlowValidationException("The selected course could not be found.");
        }

        var file = form.File!;
        var stored = await _storage.SaveAsync(file, validation.Extension);

        var originalName = SanitizeFileName(file.FileName);
        var material = new StudyMaterial
        {
            UserId = _currentUser.UserId,
            CourseId = course.Id,
            Title = form.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
            Category = form.Category,
            OriginalFileName = originalName,
            StoredFileName = stored.RelativePath,
            ContentType = stored.ContentType,
            FileExtension = stored.Extension,
            FileSizeBytes = stored.SizeBytes,
            UploadedAt = DateTime.UtcNow
        };

        try
        {
            await _materials.AddAsync(material);
        }
        catch
        {
            await _storage.DeleteAsync(stored.RelativePath);
            throw;
        }

        return material;
    }

    public async Task DeleteAsync(string id)
    {
        var userId = _currentUser.UserId;
        var material = await _materials.GetByIdAsync(userId, id)
            ?? throw new StudyFlowValidationException("This material no longer exists.");

        await _materials.DeleteAsync(userId, material.Id);
        await _storage.DeleteAsync(material.StoredFileName);
    }

    private static string SanitizeFileName(string fileName)
    {
        // Never use this value for storage paths; it is display-only.
        var name = Path.GetFileName(fileName);
        var cleaned = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) cleaned = "study-material";
        return cleaned.Length <= 180 ? cleaned : cleaned[^180..];
    }
}
