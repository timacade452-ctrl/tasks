using StudyFlow.Models;

namespace StudyFlow.Repositories;

public class InMemoryTaskRepository : InMemoryRepository<AcademicTask>, ITaskRepository
{
    public InMemoryTaskRepository() : base(t => t.Id, t => t.UserId) { }

    public Task<IReadOnlyList<AcademicTask>> GetAllAsync(string userId) => GetForUserAsync(userId);

    public async Task<IReadOnlyList<AcademicTask>> GetByCourseAsync(string userId, string courseId) =>
        (await GetForUserAsync(userId)).Where(t => t.CourseId == courseId).ToList();

    public Task<AcademicTask?> GetByIdAsync(string userId, string id) => FindForUserAsync(userId, id);

    public Task AddAsync(AcademicTask task) => base.AddItemAsync(task);

    public Task UpdateAsync(AcademicTask task) => base.UpdateItemAsync(task);

    public Task<bool> DeleteAsync(string userId, string id) => base.RemoveItemAsync(userId, id);

    public async Task<int> CountAsync(string userId) => (await GetForUserAsync(userId)).Count;

    public Task SeedAsync(IEnumerable<AcademicTask> tasks) => base.SeedItemsAsync(tasks);
}
