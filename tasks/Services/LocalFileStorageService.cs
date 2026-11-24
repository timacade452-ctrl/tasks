using Microsoft.Extensions.Options;

namespace StudyFlow.Services;

/// <summary>
/// Local demo storage. The interface is intentionally storage-agnostic so a
/// Supabase Storage implementation can replace this class in the future.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;
    private readonly FileStorageOptions _options;

    public LocalFileStorageService(IWebHostEnvironment environment, IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        _root = Path.GetFullPath(Path.Combine(webRoot, _options.RootPath));
        Directory.CreateDirectory(Path.Combine(_root, _options.MaterialsFolder));
    }

    public long MaxFileSizeBytes => _options.MaxFileSizeBytes;

    public async Task<StoredFile> SaveAsync(IFormFile file, string extension, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0 || file.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("The file size is outside the allowed range.");
        }

        extension = NormalizeExtension(extension);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = $"{_options.MaterialsFolder}/{storedName}";
        var destination = GetPhysicalPath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        try
        {
            await using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(target, cancellationToken);
        }
        catch
        {
            if (File.Exists(destination)) File.Delete(destination);
            throw;
        }

        return new StoredFile(relativePath, file.Length, FileTypes.ContentTypeFor(extension), extension);
    }

    public async Task<StoredFile> SaveBytesAsync(byte[] content, string extension, CancellationToken cancellationToken = default)
    {
        if (content.Length == 0 || content.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("The demo file size is outside the allowed range.");
        }

        extension = NormalizeExtension(extension);
        var relativePath = $"{_options.MaterialsFolder}/{Guid.NewGuid():N}{extension}";
        var destination = GetPhysicalPath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await File.WriteAllBytesAsync(destination, content, cancellationToken);

        return new StoredFile(relativePath, content.Length, FileTypes.ContentTypeFor(extension), extension);
    }

    public Stream OpenRead(string relativePath)
    {
        var fullPath = GetPhysicalPath(relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The stored file could not be found.", relativePath);
        }

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public bool Exists(string relativePath)
    {
        try
        {
            return File.Exists(GetPhysicalPath(relativePath));
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = GetPhysicalPath(relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (InvalidOperationException)
        {
            // Never delete outside the configured upload root, even if data is corrupted.
        }

        return Task.CompletedTask;
    }

    public string GetPhysicalPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidOperationException("A storage path is required.");
        }

        // Reject rooted paths and traversal sequences before resolving the path.
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(_root, normalized));
        var rootWithSeparator = _root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        return fullPath;
    }

    private static string NormalizeExtension(string extension)
    {
        var normalized = extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
        if (!FileTypes.AllowedExtensions.Contains(normalized))
        {
            throw new InvalidOperationException("Unsupported file extension.");
        }

        return normalized;
    }
}
