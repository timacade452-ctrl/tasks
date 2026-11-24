using System.Globalization;
using StudyFlow.Helpers;
using StudyFlow.Models;
using StudyFlow.Repositories;
using StudyFlow.ViewModels;

namespace StudyFlow.Services;

public class PlannerService : IPlannerService
{
    private readonly IStudyPlanRepository _plans;
    private readonly ICourseRepository _courses;
    private readonly ITaskRepository _tasks;
    private readonly ICurrentUserService _currentUser;

    public PlannerService(
        IStudyPlanRepository plans,
        ICourseRepository courses,
        ITaskRepository tasks,
        ICurrentUserService currentUser)
    {
        _plans = plans;
        _courses = courses;
        _tasks = tasks;
        _currentUser = currentUser;
    }

    public async Task<PlannerViewModel> GetPlannerAsync(DateTime date, string? editId = null)
    {
        date = date.Date;
        var userId = _currentUser.UserId;
        var courses = (await _courses.GetAllAsync(userId)).OrderBy(c => c.Code).ToList();
        var courseLookup = courses.ToDictionary(c => c.Id);
        var tasks = await _tasks.GetAllAsync(userId);

        var weekStart = StartOfWeek(date);
        var weekEnd = weekStart.AddDays(7);
        var weekPlans = (await _plans.GetRangeAsync(userId, weekStart, weekEnd))
            .OrderBy(p => p.Start)
            .ToList();

        var dayPlans = weekPlans
            .Where(p => p.Start < date.AddDays(1) && p.End > date)
            .OrderBy(p => p.Start)
            .ToList();

        var dueTasks = tasks
            .Where(t => t.DueDate.Date == date)
            .OrderBy(t => t.Status == AcademicTaskStatus.Completed)
            .ThenBy(t => t.Priority)
            .ToList();

        var upcoming = (await _plans.GetAllAsync(userId))
            .Where(p => p.Start >= DateTime.Now.AddMinutes(-30))
            .OrderBy(p => p.Start)
            .Take(5)
            .ToList();

        var form = editId is null ? BuildNewForm(date, courses) : await BuildEditFormAsync(editId, courses, date);

        var today = DateTime.Today;
        var weekDays = Enumerable.Range(0, 7).Select(offset =>
        {
            var day = weekStart.AddDays(offset);
            return new PlannerDaySummary
            {
                Date = day,
                PlanCount = weekPlans.Count(p => p.Start < day.AddDays(1) && p.End > day),
                DeadlineCount = tasks.Count(t => t.DueDate.Date == day && t.Status != AcademicTaskStatus.Completed),
                IsToday = day == today,
                IsSelected = day == date
            };
        }).ToList();

        return new PlannerViewModel
        {
            Date = date,
            DateLabel = date == today
                ? $"Today · {date.ToString("dddd, MMMM d", CultureInfo.InvariantCulture)}"
                : date.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture),
            Plans = MapPlans(dayPlans, courseLookup),
            DueTasks = dueTasks.Select(t => new TaskListItemViewModel
            {
                Task = t,
                Course = t.CourseId is not null ? courseLookup.GetValueOrDefault(t.CourseId) : null
            }).ToList(),
            WeekDays = weekDays,
            Upcoming = MapPlans(upcoming, courseLookup),
            Courses = courses,
            Form = form,
            PlannedMinutes = dayPlans.Sum(p => (int)(p.End - p.Start).TotalMinutes),
            PreviousDate = date.AddDays(-1),
            NextDate = date.AddDays(1)
        };
    }

    public async Task<CalendarViewModel> GetCalendarAsync(string view, DateTime anchor)
    {
        var normalized = (view ?? "month").Trim().ToLowerInvariant();
        if (normalized is not ("month" or "week" or "day")) normalized = "month";

        var userId = _currentUser.UserId;
        var courses = (await _courses.GetAllAsync(userId)).ToDictionary(c => c.Id);
        var tasks = await _tasks.GetAllAsync(userId);
        var today = DateTime.Today;
        anchor = anchor.Date;

        DateTime rangeStart;
        DateTime rangeEnd;
        DateTime previous;
        DateTime next;
        string title;

        switch (normalized)
        {
            case "week":
                rangeStart = StartOfWeek(anchor);
                rangeEnd = rangeStart.AddDays(7);
                previous = anchor.AddDays(-7);
                next = anchor.AddDays(7);
                title = rangeStart.Month == rangeEnd.AddDays(-1).Month
                    ? $"{rangeStart:MMM d} – {rangeEnd.AddDays(-1):d, yyyy}"
                    : $"{rangeStart:MMM d} – {rangeEnd.AddDays(-1):MMM d, yyyy}";
                break;
            case "day":
                rangeStart = anchor;
                rangeEnd = anchor.AddDays(1);
                previous = anchor.AddDays(-1);
                next = anchor.AddDays(1);
                title = anchor.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);
                break;
            default:
                var firstOfMonth = new DateTime(anchor.Year, anchor.Month, 1);
                rangeStart = StartOfWeek(firstOfMonth);
                rangeEnd = rangeStart.AddDays(42);
                previous = firstOfMonth.AddMonths(-1);
                next = firstOfMonth.AddMonths(1);
                title = firstOfMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
                break;
        }

        var plans = (await _plans.GetRangeAsync(userId, rangeStart, rangeEnd))
            .OrderBy(p => p.Start)
            .ToList();

        var withCourse = plans.Select(p => new PlanListItemViewModel
        {
            Plan = p,
            Course = p.CourseId is not null ? courses.GetValueOrDefault(p.CourseId) : null
        }).ToList();

        CalendarDayViewModel BuildDay(DateTime day, bool currentMonth)
        {
            var dayPlans = withCourse
                .Where(vm => vm.Plan.Start < day.AddDays(1) && vm.Plan.End > day)
                .OrderBy(vm => vm.Plan.Start)
                .ToList();

            var deadlines = tasks
                .Where(t => t.DueDate.Date == day && t.Status != AcademicTaskStatus.Completed)
                .OrderBy(t => t.Priority)
                .Select(t => new TaskListItemViewModel
                {
                    Task = t,
                    Course = t.CourseId is not null ? courses.GetValueOrDefault(t.CourseId) : null
                })
                .ToList();

            return new CalendarDayViewModel
            {
                Date = day,
                IsCurrentMonth = currentMonth,
                IsToday = day == today,
                Plans = dayPlans,
                Deadlines = deadlines
            };
        }

        List<CalendarDayViewModel> days;
        if (normalized == "month")
        {
            days = Enumerable.Range(0, 42)
                .Select(i => rangeStart.AddDays(i))
                .Select(day => BuildDay(day, day.Month == anchor.Month && day.Year == anchor.Year))
                .ToList();
        }
        else if (normalized == "week")
        {
            days = Enumerable.Range(0, 7).Select(i => BuildDay(rangeStart.AddDays(i), true)).ToList();
        }
        else
        {
            days = [BuildDay(anchor, true)];
        }

        var selected = days.FirstOrDefault(d => d.Date == anchor)
            ?? BuildDay(anchor, true);

        var weekStart = StartOfWeek(anchor);
        var weekDays = Enumerable.Range(0, 7).Select(i => BuildDay(weekStart.AddDays(i), true)).ToList();

        return new CalendarViewModel
        {
            View = normalized,
            Anchor = anchor,
            Title = title,
            Days = days,
            SelectedDay = selected,
            WeekDays = weekDays,
            PreviousAnchor = previous,
            NextAnchor = next,
            MonthPlanCount = plans.Count,
            MonthDeadlineCount = tasks.Count(t => t.Status != AcademicTaskStatus.Completed &&
                                                   t.DueDate >= rangeStart && t.DueDate < rangeEnd)
        };
    }

    public async Task<PlanFormViewModel?> GetFormAsync(string id)
    {
        var plan = await _plans.GetByIdAsync(_currentUser.UserId, id);
        if (plan is null) return null;
        var form = PlanFormViewModel.FromPlan(plan);
        form.Courses = (await _courses.GetAllAsync(_currentUser.UserId)).OrderBy(c => c.Code).ToList();
        return form;
    }

    public async Task<StudyPlanEvent> CreateAsync(PlanFormViewModel form)
    {
        await ValidateCourseAsync(form.CourseId);

        var plan = new StudyPlanEvent
        {
            UserId = _currentUser.UserId,
            CourseId = string.IsNullOrWhiteSpace(form.CourseId) ? null : form.CourseId,
            Title = form.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
            Type = form.Type,
            Start = form.StartDateTime,
            End = form.EndDateTime,
            Location = string.IsNullOrWhiteSpace(form.Location) ? null : form.Location.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _plans.AddAsync(plan);
        return plan;
    }

    public async Task UpdateAsync(string id, PlanFormViewModel form)
    {
        var userId = _currentUser.UserId;
        var plan = await _plans.GetByIdAsync(userId, id)
            ?? throw new StudyFlowValidationException("This study plan entry no longer exists.");

        await ValidateCourseAsync(form.CourseId);

        plan.CourseId = string.IsNullOrWhiteSpace(form.CourseId) ? null : form.CourseId;
        plan.Title = form.Title.Trim();
        plan.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        plan.Type = form.Type;
        plan.Start = form.StartDateTime;
        plan.End = form.EndDateTime;
        plan.Location = string.IsNullOrWhiteSpace(form.Location) ? null : form.Location.Trim();

        await _plans.UpdateAsync(plan);
    }

    public async Task DeleteAsync(string id)
    {
        var userId = _currentUser.UserId;
        var plan = await _plans.GetByIdAsync(userId, id)
            ?? throw new StudyFlowValidationException("This study plan entry no longer exists.");
        await _plans.DeleteAsync(userId, plan.Id);
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

    private async Task<PlanFormViewModel> BuildEditFormAsync(string id, IReadOnlyList<Course> courses, DateTime date)
    {
        var plan = await _plans.GetByIdAsync(_currentUser.UserId, id);
        if (plan is null) return BuildNewForm(date, courses);

        var form = PlanFormViewModel.FromPlan(plan);
        form.Courses = courses;
        return form;
    }

    private static PlanFormViewModel BuildNewForm(DateTime date, IReadOnlyList<Course> courses)
    {
        var start = date.Date.AddHours(9);
        if (date == DateTime.Today)
        {
            var now = DateTime.Now;
            var minutes = now.Minute < 30 ? 30 : 0;
            var hour = now.Minute < 30 ? now.Hour : now.Hour + 1;
            start = now.Date.AddHours(hour).AddMinutes(minutes);
            if (start < now.AddMinutes(15)) start = start.AddMinutes(30);
            if (start.Hour >= 22) start = now.Date.AddHours(9);
        }

        return new PlanFormViewModel
        {
            Date = date,
            StartTime = start.ToString("HH:mm", CultureInfo.InvariantCulture),
            EndTime = start.AddHours(1).ToString("HH:mm", CultureInfo.InvariantCulture),
            Courses = courses
        };
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-diff);
    }

    private static List<PlanListItemViewModel> MapPlans(
        IEnumerable<StudyPlanEvent> plans,
        IReadOnlyDictionary<string, Course> courseLookup) =>
        plans.Select(p => new PlanListItemViewModel
        {
            Plan = p,
            Course = p.CourseId is not null ? courseLookup.GetValueOrDefault(p.CourseId) : null
        }).ToList();
}
