using StudyFlow.Repositories;

namespace StudyFlow.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the demo implementations behind interfaces. To move to Supabase,
    /// replace the repository registrations with Supabase-backed implementations;
    /// controllers, services and views remain unchanged.
    /// </summary>
    public static IServiceCollection AddStudyFlow(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        services.AddSingleton<ICurrentUserService, DemoCurrentUserService>();

        // Demo persistence (in-memory). These are singletons because the data must
        // outlive a request in the current no-database version.
        services.AddSingleton<InMemoryCourseRepository>();
        services.AddSingleton<InMemoryTaskRepository>();
        services.AddSingleton<InMemoryMaterialRepository>();
        services.AddSingleton<InMemoryPomodoroRepository>();
        services.AddSingleton<InMemoryStudyPlanRepository>();
        services.AddSingleton<InMemoryUserProfileRepository>();

        services.AddSingleton<ICourseRepository>(sp => sp.GetRequiredService<InMemoryCourseRepository>());
        services.AddSingleton<ITaskRepository>(sp => sp.GetRequiredService<InMemoryTaskRepository>());
        services.AddSingleton<IMaterialRepository>(sp => sp.GetRequiredService<InMemoryMaterialRepository>());
        services.AddSingleton<IPomodoroRepository>(sp => sp.GetRequiredService<InMemoryPomodoroRepository>());
        services.AddSingleton<IStudyPlanRepository>(sp => sp.GetRequiredService<InMemoryStudyPlanRepository>());
        services.AddSingleton<IUserProfileRepository>(sp => sp.GetRequiredService<InMemoryUserProfileRepository>());

        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddSingleton<IFileValidationService, FileValidationService>();

        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<IPomodoroService, PomodoroService>();
        services.AddScoped<IPlannerService, PlannerService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<LayoutDataFilter>();

        return services;
    }

    public static async Task InitializeStudyFlowAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync();
    }
}
