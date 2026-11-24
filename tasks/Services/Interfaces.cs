using StudyFlow.Models;
using StudyFlow.ViewModels;

namespace StudyFlow.Services;

public interface IUserProfileService
{
    Task<UserProfile> GetAsync();
    Task UpdateProfileAsync(ProfileFormViewModel form);
    Task UpdateSettingsAsync(SettingsViewModel form);
}

public interface ICourseService
{
    Task<IReadOnlyList<Course>> GetAllAsync();
    Task<IReadOnlyList<CourseSummaryViewModel>> GetSummariesAsync();
    Task<Course?> GetAsync(string id);
    Task<CourseFormViewModel?> GetFormAsync(string id);
    Task<Course> CreateAsync(CourseFormViewModel form);
    Task UpdateAsync(string id, CourseFormViewModel form);
    Task DeleteAsync(string id);
    Task<bool> CodeExistsAsync(string code, string? excludeId = null);
    Task<CourseDetailsViewModel?> GetDetailsAsync(string id);
}

public interface ITaskService
{
    Task<TaskListViewModel> GetListAsync(TaskFilterViewModel filter);
    Task<AcademicTask?> GetAsync(string id);
    Task<TaskFormViewModel?> GetFormAsync(string id);
    Task<TaskDetailsViewModel?> GetDetailsAsync(string id);
    Task<IReadOnlyList<TaskListItemViewModel>> GetActiveAsync();
    Task<IReadOnlyList<TaskListItemViewModel>> GetUpcomingAsync(int take);
    Task<int> CountPendingAsync();
    Task<AcademicTask> CreateAsync(TaskFormViewModel form);
    Task UpdateAsync(string id, TaskFormViewModel form);
    Task DeleteAsync(string id);
    Task<AcademicTaskStatus> ToggleCompleteAsync(string id);
    Task UpdateStatusAsync(string id, AcademicTaskStatus status);
}

public interface IMaterialService
{
    Task<MaterialListViewModel> GetListAsync(MaterialFilterViewModel filter);
    Task<StudyMaterial?> GetAsync(string id);
    Task<MaterialPreviewViewModel?> GetPreviewAsync(string id);
    Task<IReadOnlyList<StudyMaterial>> GetByCourseAsync(string courseId);
    Task<StudyMaterial> UploadAsync(UploadMaterialViewModel form);
    Task DeleteAsync(string id);
}

public interface IPomodoroService
{
    Task<PomodoroRecordResult> RecordAsync(RecordPomodoroRequest request);
    Task<IReadOnlyList<PomodoroSessionViewModel>> GetRecentAsync(int take = 8);
}

public interface IPlannerService
{
    Task<PlannerViewModel> GetPlannerAsync(DateTime date, string? editId = null);
    Task<CalendarViewModel> GetCalendarAsync(string view, DateTime anchor);
    Task<PlanFormViewModel?> GetFormAsync(string id);
    Task<StudyPlanEvent> CreateAsync(PlanFormViewModel form);
    Task UpdateAsync(string id, PlanFormViewModel form);
    Task DeleteAsync(string id);
}

public interface IAnalyticsService
{
    Task<StudySummary> GetSummaryAsync();
    Task<DashboardViewModel> GetDashboardAsync();
    Task<AnalyticsViewModel> GetAnalyticsAsync();
    Task<IReadOnlyList<ActivityItem>> GetRecentActivityAsync(int take = 6);
    Task<IReadOnlyList<DailyStudyPoint>> GetDailySeriesAsync(int days);
    Task<int> GetStreakAsync();
}
