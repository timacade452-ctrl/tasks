using StudyFlow.Models;

namespace StudyFlow.Repositories;

public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> GetAllAsync(string userId);
    Task<Course?> GetByIdAsync(string userId, string id);
    Task AddAsync(Course course);
    Task UpdateAsync(Course course);
    Task<bool> DeleteAsync(string userId, string id);
    Task<bool> CodeExistsAsync(string userId, string code, string? excludeId = null);
    Task<int> CountAsync(string userId);
}
