using StudyFlow.Models;

namespace StudyFlow.Repositories;

public class InMemoryPomodoroRepository : InMemoryRepository<PomodoroSession>, IPomodoroRepository
{
    public InMemoryPomodoroRepository() : base(s => s.Id, s => s.UserId) { }

    public Task<IReadOnlyList<PomodoroSession>> GetAllAsync(string userId) => GetForUserAsync(userId);

    public async Task<IReadOnlyList<PomodoroSession>> GetRangeAsync(string userId, DateTime from, DateTime to) =>
        (await GetForUserAsync(userId))
            .Where(s => s.CompletedAt >= from && s.CompletedAt <= to)
            .ToList();

    public Task AddAsync(PomodoroSession session) => base.AddItemAsync(session);

    public Task UpdateAsync(PomodoroSession session) => base.UpdateItemAsync(session);

    public Task<bool> DeleteAsync(string userId, string id) => base.RemoveItemAsync(userId, id);

    public Task SeedAsync(IEnumerable<PomodoroSession> sessions) => base.SeedItemsAsync(sessions);

    public Task RemoveAllAsync(string userId) => RemoveAllForUserAsync(userId);
}

