using System.ComponentModel.DataAnnotations;

namespace StudyFlow.Models;

public enum TaskPriority
{
    [Display(Name = "Low")] Low = 0,
    [Display(Name = "Medium")] Medium = 1,
    [Display(Name = "High")] High = 2
}

public enum AcademicTaskStatus
{
    [Display(Name = "To Do")] ToDo = 0,
    [Display(Name = "In Progress")] InProgress = 1,
    [Display(Name = "Completed")] Completed = 2
}

public enum AcademicTaskType
{
    [Display(Name = "Assignment")] Assignment = 0,
    [Display(Name = "Homework")] Homework = 1,
    [Display(Name = "Exam prep")] ExamPrep = 2,
    [Display(Name = "Reading")] Reading = 3,
    [Display(Name = "Project")] Project = 4,
    [Display(Name = "Personal study")] Personal = 5
}

public enum MaterialCategory
{
    [Display(Name = "Lecture slides")] LectureSlides = 0,
    [Display(Name = "Lecture notes")] LectureNotes = 1,
    [Display(Name = "Syllabus")] Syllabus = 2,
    [Display(Name = "Assignment brief")] AssignmentBrief = 3,
    [Display(Name = "Reading")] Reading = 4,
    [Display(Name = "Reference")] Reference = 5,
    [Display(Name = "Other")] Other = 6
}

public enum StudyPlanType
{
    [Display(Name = "Study session")] StudySession = 0,
    [Display(Name = "Lecture")] Lecture = 1,
    [Display(Name = "Exam")] Exam = 2,
    [Display(Name = "Assignment deadline")] Deadline = 3,
    [Display(Name = "Course activity")] CourseActivity = 4,
    [Display(Name = "Personal")] Personal = 5
}

public enum PomodoroPhase
{
    [Display(Name = "Focus")] Focus = 0,
    [Display(Name = "Short break")] ShortBreak = 1,
    [Display(Name = "Long break")] LongBreak = 2
}
