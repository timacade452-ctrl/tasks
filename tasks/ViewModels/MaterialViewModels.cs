using System.ComponentModel.DataAnnotations;
using StudyFlow.Models;

namespace StudyFlow.ViewModels;

public class MaterialFilterViewModel
{
    public string? Q { get; set; }
    public string? CourseId { get; set; }
    /// <summary>pdf, word or slides</summary>
    public string? Kind { get; set; }
    public MaterialCategory? Category { get; set; }
    public string Sort { get; set; } = "newest";

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Q) || !string.IsNullOrWhiteSpace(CourseId) ||
        !string.IsNullOrWhiteSpace(Kind) || Category.HasValue || Sort != "newest";
}

public class MaterialListItemViewModel
{
    public StudyMaterial Material { get; init; } = new();
    public Course? Course { get; init; }
}

public class MaterialListViewModel
{
    public IReadOnlyList<MaterialListItemViewModel> Materials { get; set; } = [];
    public IReadOnlyList<Course> Courses { get; set; } = [];
    public MaterialFilterViewModel Filters { get; set; } = new();
    public int PdfCount { get; set; }
    public int WordCount { get; set; }
    public int SlidesCount { get; set; }
    public long TotalSize { get; set; }
}

public class UploadMaterialViewModel
{
    [Required(ErrorMessage = "Document title is required.")]
    [StringLength(140, MinimumLength = 3, ErrorMessage = "Title must be 3–140 characters.")]
    [Display(Name = "Document title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Select a course for this document.")]
    [Display(Name = "Course")]
    public string? CourseId { get; set; }

    [Display(Name = "Category")]
    public MaterialCategory Category { get; set; } = MaterialCategory.LectureNotes;

    [Required(ErrorMessage = "Choose a file to upload.")]
    [Display(Name = "File")]
    public IFormFile? File { get; set; }

    public IReadOnlyList<Course> Courses { get; set; } = [];
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;
    public string MaxFileSizeLabel => Helpers.DisplayHelpers.FileSize(MaxFileSizeBytes);
}

public class MaterialPreviewViewModel
{
    public StudyMaterial Material { get; set; } = new();
    public Course? Course { get; set; }
    public IReadOnlyList<MaterialListItemViewModel> Related { get; set; } = [];
}
