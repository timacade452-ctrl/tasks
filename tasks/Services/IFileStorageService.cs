namespace StudyFlow.Services;

public record StoredFile(string RelativePath, long SizeBytes, string ContentType, string Extension);

public interface IFileStorageService
{
    long MaxFileSizeBytes { get; }

    Task<StoredFile> SaveAsync(IFormFile file, string extension, CancellationToken cancellationToken = default);

    Task<StoredFile> SaveBytesAsync(byte[] content, string extension, CancellationToken cancellationToken = default);

    Stream OpenRead(string relativePath);

    bool Exists(string relativePath);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    string GetPhysicalPath(string relativePath);
}

public record FileValidationResult(bool IsValid, string? Error, string Extension, string ContentType)
{
    public static FileValidationResult Ok(string extension) =>
        new(true, null, extension, FileTypes.ContentTypeFor(extension));

    public static FileValidationResult Fail(string error) =>
        new(false, error, string.Empty, "application/octet-stream");
}

public interface IFileValidationService
{
    FileValidationResult Validate(IFormFile? file);
}
