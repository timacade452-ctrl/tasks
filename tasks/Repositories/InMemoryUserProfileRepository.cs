using System.Collections.Concurrent;
using StudyFlow.Models;

namespace StudyFlow.Repositories;

public class InMemoryUserProfileRepository : IUserProfileRepository
{
    private readonly ConcurrentDictionary<string, UserProfile> _profiles = new();

    public Task<UserProfile?> GetAsync(string userId) =>
        Task.FromResult(_profiles.TryGetValue(userId, out var profile) ? profile : null);

    public Task SaveAsync(UserProfile profile)
    {
        _profiles[profile.Id] = profile;
        return Task.CompletedTask;
    }

    public Task SeedAsync(UserProfile profile)
    {
        _profiles.TryAdd(profile.Id, profile);
        return Task.CompletedTask;
    }
}
