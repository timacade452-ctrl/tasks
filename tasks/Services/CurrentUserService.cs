namespace StudyFlow.Services;

public interface ICurrentUserService
{
    /// <summary>
    /// The demo runs as a single local student. Replacing this with Supabase
    /// Auth claims later gives every query a real user scope automatically.
    /// </summary>
    string UserId { get; }
}

public class DemoCurrentUserService : ICurrentUserService
{
    public const string DemoUserId = "demo-user";
    public string UserId => DemoUserId;
}
