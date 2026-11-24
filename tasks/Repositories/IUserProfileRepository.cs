using StudyFlow.Models;

namespace StudyFlow.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetAsync(string userId);
    Task SaveAsync(UserProfile profile);
}
