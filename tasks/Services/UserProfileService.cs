using StudyFlow.Models;
using StudyFlow.Repositories;
using StudyFlow.ViewModels;

namespace StudyFlow.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IUserProfileRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public UserProfileService(IUserProfileRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<UserProfile> GetAsync()
    {
        var profile = await _repository.GetAsync(_currentUser.UserId);
        if (profile is not null) return profile;

        profile = new UserProfile { Id = _currentUser.UserId };
        await _repository.SaveAsync(profile);
        return profile;
    }

    public async Task UpdateProfileAsync(ProfileFormViewModel form)
    {
        var profile = await GetAsync();
        profile.FullName = form.FullName.Trim();
        profile.Email = form.Email.Trim();
        profile.University = form.University.Trim();
        profile.Program = form.Program.Trim();
        profile.YearOfStudy = form.YearOfStudy.Trim();
        profile.Bio = string.IsNullOrWhiteSpace(form.Bio) ? null : form.Bio.Trim();
        await _repository.SaveAsync(profile);
    }

    public async Task UpdateSettingsAsync(SettingsViewModel form)
    {
        var profile = await GetAsync();
        profile.DailyGoalMinutes = form.DailyGoalMinutes;
        profile.FocusMinutes = form.FocusMinutes;
        profile.ShortBreakMinutes = form.ShortBreakMinutes;
        profile.LongBreakMinutes = form.LongBreakMinutes;
        profile.LongBreakInterval = form.LongBreakInterval;
        profile.SoundEnabled = form.SoundEnabled;
        profile.AutoStartBreaks = form.AutoStartBreaks;
        await _repository.SaveAsync(profile);
    }
}
