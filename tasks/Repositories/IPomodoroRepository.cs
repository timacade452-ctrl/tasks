using StudyFlow.Models;

namespace StudyFlow.Repositories;

public interface IPomodoroRepository
{
    Task<IReadOnlyList<PomodoroSession>> GetAllAsync(string userId);
    Task<IReadOnlyList<PomodoroSession>> GetRangeAsync(string userId, DateTime from, DateTime to);
    Task AddAsync(PomodoroSession session);
    Task UpdateAsync(PomodoroSession session);
    Task<bool> DeleteAsync(string userId, string id);
}

