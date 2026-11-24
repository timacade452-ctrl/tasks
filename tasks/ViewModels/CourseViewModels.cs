using System.ComponentModel.DataAnnotations;
using StudyFlow.Models;

namespace StudyFlow.ViewModels;

public class CourseFormViewModel
{
    public string? Id { get; set; }
    public bool IsEdit => !string.IsNullOrEmpty(Id);

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
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Choose a valid course color.")]
    public string Color { get; set; } = "#4f46e5";

    [Required]
    [StringLength(40)]
    public string Icon { get; set; } = "book-open";

    public static CourseFormViewModel FromCourse(Course course) => new()
    {
        Id = course.Id,
        Code = course.Code,
        Name = course.Name,
        Instructor = course.Instructor,
        Semester = course.Semester,
        Description = course.Description,
        Color = course.Color,
        Icon = course.Icon
    };
}

public class CoursesIndexViewModel
{
    public IReadOnlyList<CourseSummaryViewModel> Courses { get; set; } = [];
    public int TotalMaterials { get; set; }
    public int OpenTasks { get; set; }
    public int FocusMinutes { get; set; }
    public int CompletedTasks { get; set; }
}

public class CourseDetailsViewModel
{
    public Course Course { get; set; } = new();
    public IReadOnlyList<TaskListItemViewModel> Tasks { get; set; } = [];
    public IReadOnlyList<StudyMaterial> Materials { get; set; } = [];
    public int FocusMinutes { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int MaterialsCount { get; set; }
    public int Progress => Helpers.DisplayHelpers.Percent(CompletedTasks, TotalTasks);
    public AcademicTask? NextDeadline =>
        Tasks.Where(t => t.Task.Status != AcademicTaskStatus.Completed)
             .OrderBy(t => t.Task.DueDate)
             .Select(t => t.Task)
             .FirstOrDefault();
}
