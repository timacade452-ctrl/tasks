using System.ComponentModel.DataAnnotations;

namespace StudyFlow.Models;

public class Course
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course code is required.")]
    [StringLength(16, MinimumLength = 2, ErrorMessage = "Course code must be 2–16 characters.")]
    [Display(Name = "Course code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Course name must be 2–100 characters.")]
    [Display(Name = "Course name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Instructor name is required.")]
    [StringLength(100, ErrorMessage = "Instructor name is too long.")]
    [Display(Name = "Instructor")]
    public string Instructor { get; set; } = string.Empty;

    [Required(ErrorMessage = "Semester is required.")]
    [StringLength(40, ErrorMessage = "Semester is too long.")]
    [Display(Name = "Semester")]
    public string Semester { get; set; } = string.Empty;

    [StringLength(600, ErrorMessage = "Description cannot exceed 600 characters.")]
    public string? Description { get; set; }

    [Required]
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Choose a valid course color.")]
    public string Color { get; set; } = "#4f46e5";

    [Required]
    [StringLength(40)]
    public string Icon { get; set; } = "book-open";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
