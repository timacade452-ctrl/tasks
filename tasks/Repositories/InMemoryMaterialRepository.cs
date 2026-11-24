using StudyFlow.Models;

namespace StudyFlow.Repositories;

public class InMemoryMaterialRepository : InMemoryRepository<StudyMaterial>, IMaterialRepository
{
    public InMemoryMaterialRepository() : base(m => m.Id, m => m.UserId) { }

    public Task<IReadOnlyList<StudyMaterial>> GetAllAsync(string userId) => GetForUserAsync(userId);

    public async Task<IReadOnlyList<StudyMaterial>> GetByCourseAsync(string userId, string courseId) =>
        (await GetForUserAsync(userId)).Where(m => m.CourseId == courseId).ToList();

    public Task<StudyMaterial?> GetByIdAsync(string userId, string id) => FindForUserAsync(userId, id);

    public Task AddAsync(StudyMaterial material) => base.AddItemAsync(material);

    public Task UpdateAsync(StudyMaterial material) => base.UpdateItemAsync(material);

    public Task<bool> DeleteAsync(string userId, string id) => base.RemoveItemAsync(userId, id);

    public async Task<int> CountAsync(string userId) => (await GetForUserAsync(userId)).Count;

    public Task SeedAsync(IEnumerable<StudyMaterial> materials) => base.SeedItemsAsync(materials);
}
