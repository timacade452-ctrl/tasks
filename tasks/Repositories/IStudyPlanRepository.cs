using StudyFlow.Models;

namespace StudyFlow.Repositories;

public interface IStudyPlanRepository
{
    Task<IReadOnlyList<StudyPlanEvent>> GetAllAsync(string userId);
    Task<IReadOnlyList<StudyPlanEvent>> GetRangeAsync(string userId, DateTime start, DateTime end);
    Task<StudyPlanEvent?> GetByIdAsync(string userId, string id);
    Task AddAsync(StudyPlanEvent plan);
    Task UpdateAsync(StudyPlanEvent plan);
    Task<bool> DeleteAsync(string userId, string id);
}
