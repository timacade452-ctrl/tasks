using StudyFlow.Helpers;
using StudyFlow.Models;
using StudyFlow.Repositories;
using StudyFlow.ViewModels;

namespace StudyFlow.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courses;
    private readonly ITaskRepository _tasks;
    private readonly IMaterialRepository _materials;
    private readonly IPomodoroRepository _sessions;
    private readonly IStudyPlanRepository _plans;
    private readonly IFileStorageService _storage;
    private readonly ICurrentUserService _currentUser;

    public CourseService(
        ICourseRepository courses,
        ITaskRepository tasks,
        IMaterialRepository materials,
        IPomodoroRepository sessions,
        IStudyPlanRepository plans,
        IFileStorageService storage,
        ICurrentUserService currentUser)
    {
        _courses = courses;
        _tasks = tasks;
        _materials = materials;
        _sessions = sessions;
        _plans = plans;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<Course>> GetAllAsync()
    {
        var courses = await _courses.GetAllAsync(_currentUser.UserId);
        return courses.OrderBy(c => c.Code).ToList();
    }

    public async Task<IReadOnlyList<CourseSummaryViewModel>> GetSummariesAsync()
    {
        var userId = _currentUser.UserId;
        var courses = await _courses.GetAllAsync(userId);
        var tasks = await _tasks.GetAllAsync(userId);
        var materials = await _materials.GetAllAsync(userId);
        var sessions = await _sessions.GetAllAsync(userId);

        return courses
            .OrderBy(c => c.Code)
            .Select(c => new CourseSummaryViewModel
            {
                Course = c,
                MaterialsCount = materials.Count(m => m.CourseId == c.Id),
                TotalTasks = tasks.Count(t => t.CourseId == c.Id),
                CompletedTasks = tasks.Count(t => t.CourseId == c.Id && t.Status == AcademicTaskStatus.Completed),
                FocusMinutes = sessions
                    .Where(s => s.CourseId == c.Id && s.Phase == PomodoroPhase.Focus)
                    .Sum(s => s.DurationMinutes)
            })
            .ToList();
    }

    public async Task<Course?> GetAsync(string id) =>
        await _courses.GetByIdAsync(_currentUser.UserId, id);

    public async Task<CourseFormViewModel?> GetFormAsync(string id)
    {
        var course = await GetAsync(id);
        return course is null ? null : CourseFormViewModel.FromCourse(course);
    }

    public async Task<Course> CreateAsync(CourseFormViewModel form)
    {
        if (await CodeExistsAsync(form.Code))
        {
            throw new StudyFlowValidationException($"A course with the code “{form.Code.Trim()}” already exists.");
        }

        var course = new Course
        {
            UserId = _currentUser.UserId,
            Code = form.Code.Trim(),
            Name = form.Name.Trim(),
            Instructor = form.Instructor.Trim(),
            Semester = form.Semester.Trim(),
            Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
            Color = CourseCatalog.HexFor(form.Color),
            Icon = CourseCatalog.IsValidIcon(form.Icon) ? form.Icon : "book-open",
            CreatedAt = DateTime.UtcNow
        };

        await _courses.AddAsync(course);
        return course;
    }

    public async Task UpdateAsync(string id, CourseFormViewModel form)
    {
        var course = await GetAsync(id)
            ?? throw new StudyFlowValidationException("This course no longer exists.");

        if (await CodeExistsAsync(form.Code, id))
        {
            throw new StudyFlowValidationException($"A course with the code “{form.Code.Trim()}” already exists.");
        }

        course.Code = form.Code.Trim();
        course.Name = form.Name.Trim();
        course.Instructor = form.Instructor.Trim();
        course.Semester = form.Semester.Trim();
        course.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        course.Color = CourseCatalog.HexFor(form.Color);
        course.Icon = CourseCatalog.IsValidIcon(form.Icon) ? form.Icon : "book-open";

        await _courses.UpdateAsync(course);
    }

    public async Task DeleteAsync(string id)
    {
        var userId = _currentUser.UserId;
        var course = await _courses.GetByIdAsync(userId, id)
            ?? throw new StudyFlowValidationException("This course no longer exists.");

        // Materials own local files, so remove both the record and the file.
        var materials = await _materials.GetByCourseAsync(userId, id);
        foreach (var material in materials)
        {
            await _materials.DeleteAsync(userId, material.Id);
            await _storage.DeleteAsync(material.StoredFileName);
        }

        var tasks = await _tasks.GetByCourseAsync(userId, id);
        foreach (var task in tasks)
        {
            await _tasks.DeleteAsync(userId, task.Id);
        }

        var plans = (await _plans.GetAllAsync(userId)).Where(p => p.CourseId == id).ToList();
        foreach (var plan in plans)
        {
            await _plans.DeleteAsync(userId, plan.Id);
        }

        // Keep focus history for analytics, but detach it from the deleted course.
        var sessions = (await _sessions.GetAllAsync(userId)).ToList();
        foreach (var session in sessions.Where(s => s.CourseId == id || tasks.Any(t => t.Id == s.TaskId)))
        {
            session.CourseId = null;
            session.TaskId = null;
            await _sessions.UpdateAsync(session);
        }

        await _courses.DeleteAsync(userId, course.Id);
    }

    public async Task<bool> CodeExistsAsync(string code, string? excludeId = null) =>
        await _courses.CodeExistsAsync(_currentUser.UserId, code, excludeId);

    public async Task<CourseDetailsViewModel?> GetDetailsAsync(string id)
    {
        var userId = _currentUser.UserId;
        var course = await _courses.GetByIdAsync(userId, id);
        if (course is null) return null;

        var tasks = (await _tasks.GetByCourseAsync(userId, id))
            .OrderBy(t => t.Status == AcademicTaskStatus.Completed)
            .ThenBy(t => t.DueDate)
            .ToList();

        var materials = (await _materials.GetByCourseAsync(userId, id))
            .OrderByDescending(m => m.UploadedAt)
            .ToList();

        var focusMinutes = (await _sessions.GetAllAsync(userId))
            .Where(s => s.CourseId == id && s.Phase == PomodoroPhase.Focus)
            .Sum(s => s.DurationMinutes);

        return new CourseDetailsViewModel
        {
            Course = course,
            Tasks = tasks.Select(t => new TaskListItemViewModel { Task = t, Course = course }).ToList(),
            Materials = materials,
            FocusMinutes = focusMinutes,
            TotalTasks = tasks.Count,
            CompletedTasks = tasks.Count(t => t.Status == AcademicTaskStatus.Completed),
            MaterialsCount = materials.Count
        };
    }
}
