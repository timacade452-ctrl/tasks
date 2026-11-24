using StudyFlow.Models;

namespace StudyFlow.Repositories;

public class InMemoryStudyPlanRepository : InMemoryRepository<StudyPlanEvent>, IStudyPlanRepository
{
    public InMemoryStudyPlanRepository() : base(p => p.Id, p => p.UserId) { }

    public Task<IReadOnlyList<StudyPlanEvent>> GetAllAsync(string userId) => GetForUserAsync(userId);

    public async Task<IReadOnlyList<StudyPlanEvent>> GetRangeAsync(string userId, DateTime start, DateTime end) =>
        (await GetForUserAsync(userId))
            .Where(p => p.Start < end && p.End > start)
            .ToList();

    public Task<StudyPlanEvent?> GetByIdAsync(string userId, string id) => FindForUserAsync(userId, id);

    public Task AddAsync(StudyPlanEvent plan) => base.AddItemAsync(plan);

    public Task UpdateAsync(StudyPlanEvent plan) => base.UpdateItemAsync(plan);

    public Task<bool> DeleteAsync(string userId, string id) => base.RemoveItemAsync(userId, id);

    public Task SeedAsync(IEnumerable<StudyPlanEvent> plans) => base.SeedItemsAsync(plans);
}
