using StudyFlow.Models;

namespace StudyFlow.Repositories;

public class InMemoryCourseRepository : InMemoryRepository<Course>, ICourseRepository
{
    public InMemoryCourseRepository() : base(c => c.Id, c => c.UserId) { }

    public Task<IReadOnlyList<Course>> GetAllAsync(string userId) => GetForUserAsync(userId);

    public Task<Course?> GetByIdAsync(string userId, string id) => FindForUserAsync(userId, id);

    public Task AddAsync(Course course) => base.AddItemAsync(course);

    public Task UpdateAsync(Course course) => base.UpdateItemAsync(course);

    public Task<bool> DeleteAsync(string userId, string id) => base.RemoveItemAsync(userId, id);

    public async Task<bool> CodeExistsAsync(string userId, string code, string? excludeId = null)
    {
        var courses = await GetForUserAsync(userId);
        return courses.Any(c =>
            c.Id != excludeId &&
            string.Equals(c.Code.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> CountAsync(string userId) => (await GetForUserAsync(userId)).Count;

    public Task SeedAsync(IEnumerable<Course> courses) => base.SeedItemsAsync(courses);
}
