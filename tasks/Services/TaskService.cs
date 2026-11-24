using StudyFlow.Helpers;
using StudyFlow.Models;
using StudyFlow.Repositories;
using StudyFlow.ViewModels;

namespace StudyFlow.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _tasks;
    private readonly ICourseRepository _courses;
    private readonly IPomodoroRepository _sessions;
    private readonly ICurrentUserService _currentUser;

    public TaskService(
        ITaskRepository tasks,
        ICourseRepository courses,
        IPomodoroRepository sessions,
        ICurrentUserService currentUser)
    {
        _tasks = tasks;
        _courses = courses;
        _sessions = sessions;
        _currentUser = currentUser;
    }

    public async Task<TaskListViewModel> GetListAsync(TaskFilterViewModel filter)
    {
        var userId = _currentUser.UserId;
        var courses = await _courses.GetAllAsync(userId);
        var courseLookup = courses.ToDictionary(c => c.Id);
        var tasks = await _tasks.GetAllAsync(userId);

        var search = filter.Q?.Trim();
        var baseQuery = tasks.Where(t =>
            (string.IsNullOrWhiteSpace(search) ||
             t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             (t.Description ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
             (t.CourseId != null && courseLookup.TryGetValue(t.CourseId, out var searchCourse) &&
              (searchCourse.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               searchCourse.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))) &&
            (string.IsNullOrWhiteSpace(filter.CourseId) || t.CourseId == filter.CourseId) &&
            (!filter.Priority.HasValue || t.Priority == filter.Priority) &&
            (!filter.Type.HasValue || t.Type == filter.Type) &&
            (!filter.OverdueOnly || t.IsOverdue))
            .ToList();

        var counts = new TaskCountsViewModel
        {
            All = baseQuery.Count,
            ToDo = baseQuery.Count(t => t.Status == AcademicTaskStatus.ToDo),
            InProgress = baseQuery.Count(t => t.Status == AcademicTaskStatus.InProgress),
            Completed = baseQuery.Count(t => t.Status == AcademicTaskStatus.Completed),
            Overdue = baseQuery.Count(t => t.IsOverdue)
        };

        var filtered = baseQuery
            .Where(t => !filter.Status.HasValue || t.Status == filter.Status)
            .ToList();

        filtered = filter.Sort switch
        {
            "dueDesc" => filtered
                .OrderBy(t => t.Status == AcademicTaskStatus.Completed)
                .ThenByDescending(t => t.DueDate)
                .ToList(),
            "priority" => filtered
                .OrderBy(t => t.Status == AcademicTaskStatus.Completed)
                .ThenByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToList(),
            "created" => filtered.OrderByDescending(t => t.CreatedAt).ToList(),
            "title" => filtered.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => filtered
                .OrderBy(t => t.Status == AcademicTaskStatus.Completed)
                .ThenBy(t => t.DueDate)
                .ThenByDescending(t => t.Priority)
                .ToList()
        };

        return new TaskListViewModel
        {
            Tasks = filtered.Select(t => new TaskListItemViewModel
            {
                Task = t,
                Course = t.CourseId is not null && courseLookup.TryGetValue(t.CourseId, out var c) ? c : null
            }).ToList(),
            Courses = courses.OrderBy(c => c.Code).ToList(),
            Filters = filter,
            Counts = counts
        };
    }

    public async Task<AcademicTask?> GetAsync(string id) =>
        await _tasks.GetByIdAsync(_currentUser.UserId, id);

    public async Task<TaskFormViewModel?> GetFormAsync(string id)
    {
        var task = await GetAsync(id);
        if (task is null) return null;

        var form = TaskFormViewModel.FromTask(task);
        form.Courses = await GetCourseOptionsAsync();
        return form;
    }

    public async Task<TaskDetailsViewModel?> GetDetailsAsync(string id)
    {
        var userId = _currentUser.UserId;
        var task = await _tasks.GetByIdAsync(userId, id);
        if (task is null) return null;

        var course = task.CourseId is null ? null : await _courses.GetByIdAsync(userId, task.CourseId);
        var sessions = (await _sessions.GetAllAsync(userId))
            .Where(s => s.TaskId == task.Id && s.Phase == PomodoroPhase.Focus)
            .OrderByDescending(s => s.CompletedAt)
            .ToList();

        var nextTask = (await _tasks.GetAllAsync(userId))
            .Where(t => t.Id != task.Id && t.Status != AcademicTaskStatus.Completed &&
                        t.DueDate >= task.DueDate &&
                        (task.CourseId == null || t.CourseId == task.CourseId))
            .OrderBy(t => t.DueDate)
            .FirstOrDefault();

        return new TaskDetailsViewModel
        {
            Task = task,
            Course = course,
            Sessions = sessions,
            FocusMinutes = sessions.Sum(s => s.DurationMinutes),
            FocusSessions = sessions.Count,
            NextTask = nextTask
        };
    }

    public async Task<IReadOnlyList<TaskListItemViewModel>> GetActiveAsync()
    {
        var userId = _currentUser.UserId;
        var courses = await _courses.GetAllAsync(userId);
        var lookup = courses.ToDictionary(c => c.Id);
        var tasks = (await _tasks.GetAllAsync(userId))
            .Where(t => t.Status != AcademicTaskStatus.Completed)
            .OrderBy(t => t.DueDate)
            .ToList();

        return tasks.Select(t => new TaskListItemViewModel
        {
            Task = t,
            Course = t.CourseId is not null && lookup.TryGetValue(t.CourseId, out var c) ? c : null
        }).ToList();
    }

    public async Task<IReadOnlyList<TaskListItemViewModel>> GetUpcomingAsync(int take)
    {
        var userId = _currentUser.UserId;
        var courses = await _courses.GetAllAsync(userId);
        var lookup = courses.ToDictionary(c => c.Id);
        var tasks = (await _tasks.GetAllAsync(userId))
            .Where(t => t.Status != AcademicTaskStatus.Completed)
            .OrderBy(t => t.DueDate)
            .Take(take)
            .ToList();

        return tasks.Select(t => new TaskListItemViewModel
        {
            Task = t,
            Course = t.CourseId is not null && lookup.TryGetValue(t.CourseId, out var c) ? c : null
        }).ToList();
    }

    public async Task<int> CountPendingAsync() =>
        (await _tasks.GetAllAsync(_currentUser.UserId)).Count(t => t.Status != AcademicTaskStatus.Completed);

    public async Task<AcademicTask> CreateAsync(TaskFormViewModel form)
    {
        await ValidateCourseAsync(form.CourseId);

        var task = new AcademicTask
        {
            UserId = _currentUser.UserId,
            Title = form.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
            CourseId = string.IsNullOrWhiteSpace(form.CourseId) ? null : form.CourseId,
            Type = form.Type,
            Priority = form.Priority,
            Status = form.Status,
            StartDate = form.StartDate.Date,
            DueDate = form.DueDate.Date,
            EstimatedMinutes = form.EstimatedMinutes,
            CreatedAt = DateTime.UtcNow
        };

        if (task.Status == AcademicTaskStatus.Completed)
        {
            task.CompletedAt = DateTime.Now;
        }

        await _tasks.AddAsync(task);
        return task;
    }

    public async Task UpdateAsync(string id, TaskFormViewModel form)
    {
        var task = await GetAsync(id)
            ?? throw new StudyFlowValidationException("This task no longer exists.");

        await ValidateCourseAsync(form.CourseId);

        var wasCompleted = task.Status == AcademicTaskStatus.Completed;
        task.Title = form.Title.Trim();
        task.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        task.CourseId = string.IsNullOrWhiteSpace(form.CourseId) ? null : form.CourseId;
        task.Type = form.Type;
        task.Priority = form.Priority;
        task.Status = form.Status;
        task.StartDate = form.StartDate.Date;
        task.DueDate = form.DueDate.Date;
        task.EstimatedMinutes = form.EstimatedMinutes;

        if (task.Status == AcademicTaskStatus.Completed && !wasCompleted)
        {
            task.CompletedAt = DateTime.Now;
        }
        else if (task.Status != AcademicTaskStatus.Completed)
        {
            task.CompletedAt = null;
        }

        await _tasks.UpdateAsync(task);
    }

    public async Task DeleteAsync(string id)
    {
        var userId = _currentUser.UserId;
        var task = await _tasks.GetByIdAsync(userId, id)
            ?? throw new StudyFlowValidationException("This task no longer exists.");

        var sessions = (await _sessions.GetAllAsync(userId)).Where(s => s.TaskId == task.Id).ToList();
        foreach (var session in sessions)
        {
            session.TaskId = null;
            await _sessions.UpdateAsync(session);
        }

        await _tasks.DeleteAsync(userId, task.Id);
    }

    public async Task<AcademicTaskStatus> ToggleCompleteAsync(string id)
    {
        var task = await GetAsync(id)
            ?? throw new StudyFlowValidationException("This task no longer exists.");

        if (task.Status == AcademicTaskStatus.Completed)
        {
            task.Status = AcademicTaskStatus.ToDo;
            task.CompletedAt = null;
        }
        else
        {
            task.Status = AcademicTaskStatus.Completed;
            task.CompletedAt = DateTime.Now;
        }

        await _tasks.UpdateAsync(task);
        return task.Status;
    }

    public async Task UpdateStatusAsync(string id, AcademicTaskStatus status)
    {
        var task = await GetAsync(id)
            ?? throw new StudyFlowValidationException("This task no longer exists.");

        task.Status = status;
        task.CompletedAt = status == AcademicTaskStatus.Completed ? (task.CompletedAt ?? DateTime.Now) : null;
        await _tasks.UpdateAsync(task);
    }

    private async Task ValidateCourseAsync(string? courseId)
    {
        if (string.IsNullOrWhiteSpace(courseId)) return;

        var course = await _courses.GetByIdAsync(_currentUser.UserId, courseId);
        if (course is null)
        {
            throw new StudyFlowValidationException("The selected course could not be found.");
        }
    }

    private async Task<IReadOnlyList<Course>> GetCourseOptionsAsync()
    {
        var courses = await _courses.GetAllAsync(_currentUser.UserId);
        return courses.OrderBy(c => c.Code).ToList();
    }
}
