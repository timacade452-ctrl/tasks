namespace StudyFlow.Services;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Folder beneath wwwroot. In production this is replaced by Supabase Storage.</summary>
    public string RootPath { get; set; } = "uploads";

    public string MaterialsFolder { get; set; } = "materials";

    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;
}
