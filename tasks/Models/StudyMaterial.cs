using System.ComponentModel.DataAnnotations;

namespace StudyFlow.Models;

public class StudyMaterial
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string CourseId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Document title is required.")]
    [StringLength(140, MinimumLength = 3, ErrorMessage = "Title must be 3–140 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    public MaterialCategory Category { get; set; } = MaterialCategory.LectureNotes;

    /// <summary>Original client filename, kept only for display and download headers.</summary>
    [Required]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Generated server-side filename/path. Never derived from user input.</summary>
    [Required]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    public string ContentType { get; set; } = "application/octet-stream";

    [Required]
    public string FileExtension { get; set; } = ".pdf";

    public long FileSizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public bool IsPdf =>
        string.Equals(FileExtension, ".pdf", StringComparison.OrdinalIgnoreCase);
}
