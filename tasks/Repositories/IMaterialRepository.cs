using StudyFlow.Models;

namespace StudyFlow.Repositories;

public interface IMaterialRepository
{
    Task<IReadOnlyList<StudyMaterial>> GetAllAsync(string userId);
    Task<IReadOnlyList<StudyMaterial>> GetByCourseAsync(string userId, string courseId);
    Task<StudyMaterial?> GetByIdAsync(string userId, string id);
    Task AddAsync(StudyMaterial material);
    Task UpdateAsync(StudyMaterial material);
    Task<bool> DeleteAsync(string userId, string id);
    Task<int> CountAsync(string userId);
}
