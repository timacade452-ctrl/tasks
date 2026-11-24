using System.Collections.Concurrent;

namespace StudyFlow.Repositories;

/// <summary>
/// Small shared base for the demo data store. All access is asynchronous so a
/// Supabase/PostgreSQL implementation can replace it without changing callers.
/// </summary>
public abstract class InMemoryRepository<T> where T : class
{
    private readonly ConcurrentDictionary<string, T> _items = new();
    private readonly Func<T, string> _id;
    private readonly Func<T, string> _userId;

    protected InMemoryRepository(Func<T, string> id, Func<T, string> userId)
    {
        _id = id;
        _userId = userId;
    }

    protected Task<IReadOnlyList<T>> GetForUserAsync(string userId) =>
        Task.FromResult<IReadOnlyList<T>>(_items.Values
            .Where(item => _userId(item) == userId)
            .ToList());

    protected Task<T?> FindAsync(string id) =>
        Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

    protected Task<T?> FindForUserAsync(string userId, string id) =>
        Task.FromResult(_items.TryGetValue(id, out var item) && _userId(item) == userId ? item : null);

    protected Task AddItemAsync(T item)
    {
        _items[_id(item)] = item;
        return Task.CompletedTask;
    }

    protected Task UpdateItemAsync(T item)
    {
        _items[_id(item)] = item;
        return Task.CompletedTask;
    }

    protected Task<bool> RemoveItemAsync(string userId, string id)
    {
        if (!_items.TryGetValue(id, out var item) || _userId(item) != userId)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_items.TryRemove(id, out _));
    }

    protected Task SeedItemsAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            _items.TryAdd(_id(item), item);
        }

        return Task.CompletedTask;
    }

    protected Task<bool> AnyForUserAsync(string userId) =>
        Task.FromResult(_items.Values.Any(item => _userId(item) == userId));

    protected Task RemoveAllForUserAsync(string userId)
    {
        foreach (var pair in _items.Where(pair => _userId(pair.Value) == userId).ToList())
        {
            _items.TryRemove(pair.Key, out _);
        }

        return Task.CompletedTask;
    }
}
