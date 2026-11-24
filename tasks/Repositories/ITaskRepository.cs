using StudyFlow.Models;

namespace StudyFlow.Repositories;

public interface ITaskRepository
{
    Task<IReadOnlyList<AcademicTask>> GetAllAsync(string userId);
    Task<IReadOnlyList<AcademicTask>> GetByCourseAsync(string userId, string courseId);
    Task<AcademicTask?> GetByIdAsync(string userId, string id);
    Task AddAsync(AcademicTask task);
    Task UpdateAsync(AcademicTask task);
    Task<bool> DeleteAsync(string userId, string id);
    Task<int> CountAsync(string userId);
}
