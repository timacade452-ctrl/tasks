namespace StudyFlow.Services;

public static class FileTypes
{
    public static readonly IReadOnlyList<string> AllowedExtensions =
        [".pdf", ".doc", ".docx", ".ppt", ".pptx"];

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    };

    public static string ContentTypeFor(string extension) =>
        ContentTypes.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";

    public static string KindLabel(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "PDF",
        ".doc" or ".docx" => "Word",
        ".ppt" or ".pptx" => "PowerPoint",
        _ => "File"
    };

    public static bool MatchesKind(string extension, string? kind) =>
        string.IsNullOrWhiteSpace(kind) || kind.ToLowerInvariant() switch
        {
            "pdf" => extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase),
            "word" => extension.Equals(".doc", StringComparison.OrdinalIgnoreCase) ||
                      extension.Equals(".docx", StringComparison.OrdinalIgnoreCase),
            "slides" => extension.Equals(".ppt", StringComparison.OrdinalIgnoreCase) ||
                        extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase),
            _ => true
        };

    public static string DownloadName(string? originalName, string extension)
    {
        var safe = string.IsNullOrWhiteSpace(originalName) ? "study-material" : Path.GetFileName(originalName);
        return string.IsNullOrWhiteSpace(Path.GetExtension(safe)) ? safe + extension : safe;
    }
}
