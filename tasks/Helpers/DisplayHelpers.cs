using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Collections.Concurrent;
using StudyFlow.Models;

namespace StudyFlow.Helpers;

public static class DisplayHelpers
{
    private static readonly ConcurrentDictionary<Type, Dictionary<Enum, string>> DisplayCache = new();

    public static string DisplayName(this Enum value)
    {
        var type = value.GetType();
        var map = DisplayCache.GetOrAdd(type, t =>
        {
            var result = new Dictionary<Enum, string>();
            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var enumValue = (Enum)field.GetValue(null)!;
                var display = field.GetCustomAttribute<DisplayAttribute>()?.Name ?? enumValue.ToString();
                result[enumValue] = display;
            }
            return result;
        });

        return map.TryGetValue(value, out var name) ? name : value.ToString();
    }

    public static string FileSize(long bytes)
    {
        if (bytes <= 0) return "0 KB";
        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{size:0} {units[unit]}"
            : $"{size:0.#} {units[unit]}";
    }

    public static string FriendlyDate(DateTime value)
    {
        var date = value.Date;
        var today = DateTime.Today;
        if (date == today) return "Today";
        if (date == today.AddDays(1)) return "Tomorrow";
        if (date == today.AddDays(-1)) return "Yesterday";
        if (date.Year == today.Year) return date.ToString("ddd, MMM d", CultureInfo.InvariantCulture);
        return date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
    }

    public static string DueLabel(DateTime due)
    {
        var days = (due.Date - DateTime.Today).Days;
        return days switch
        {
            < 0 => $"{Math.Abs(days)} day{(Math.Abs(days) == 1 ? "" : "s")} overdue",
            0 => "Due today",
            1 => "Due tomorrow",
            _ => $"Due in {days} days"
        };
    }

    public static string Time(DateTime value) =>
        value.ToString("h:mm tt", CultureInfo.InvariantCulture);

    public static string Time(TimeSpan value) =>
        DateTime.Today.Add(value).ToString("h:mm tt", CultureInfo.InvariantCulture);

    public static string DateTimeLabel(DateTime value)
    {
        if (value.Date == DateTime.Today) return $"Today · {Time(value)}";
        if (value.Date == DateTime.Today.AddDays(-1)) return $"Yesterday · {Time(value)}";
        return $"{FriendlyDate(value)} · {Time(value)}";
    }

    public static string RelativeTime(DateTime value)
    {
        var span = DateTime.Now - value;
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} day{(span.TotalDays >= 2 ? "s" : "")} ago";
        return FriendlyDate(value);
    }

    public static string Greeting()
    {
        var hour = DateTime.Now.Hour;
        return hour switch
        {
            < 12 => "Good morning",
            < 18 => "Good afternoon",
            _ => "Good evening"
        };
    }

    public static string MinutesLabel(int minutes)
    {
        if (minutes < 60) return $"{minutes} min";
        var hours = minutes / 60;
        var remainder = minutes % 60;
        return remainder == 0 ? $"{hours} hr" : $"{hours}h {remainder}m";
    }

    public static double Hours(int minutes) => Math.Round(minutes / 60d, 1);

    public static string PriorityBadge(TaskPriority priority) => priority switch
    {
        TaskPriority.High => "bg-rose-50 text-rose-700 ring-rose-200",
        TaskPriority.Medium => "bg-amber-50 text-amber-700 ring-amber-200",
        _ => "bg-slate-100 text-slate-600 ring-slate-200"
    };

    public static string StatusBadge(AcademicTaskStatus status) => status switch
    {
        AcademicTaskStatus.Completed => "bg-emerald-50 text-emerald-700 ring-emerald-200",
        AcademicTaskStatus.InProgress => "bg-blue-50 text-blue-700 ring-blue-200",
        _ => "bg-slate-100 text-slate-600 ring-slate-200"
    };

    public static string PlanTone(StudyPlanType type) => type switch
    {
        StudyPlanType.Exam => "rose",
        StudyPlanType.Deadline => "amber",
        StudyPlanType.Lecture => "blue",
        StudyPlanType.CourseActivity => "violet",
        StudyPlanType.Personal => "slate",
        _ => "indigo"
    };

    public static string PlanDotClass(StudyPlanType type) => PlanTone(type) switch
    {
        "rose" => "bg-rose-500",
        "amber" => "bg-amber-500",
        "blue" => "bg-blue-500",
        "violet" => "bg-violet-500",
        "slate" => "bg-slate-400",
        _ => "bg-indigo-500"
    };

    public static string PlanSoftClass(StudyPlanType type) => PlanTone(type) switch
    {
        "rose" => "bg-rose-50 text-rose-700 ring-rose-200",
        "amber" => "bg-amber-50 text-amber-700 ring-amber-200",
        "blue" => "bg-blue-50 text-blue-700 ring-blue-200",
        "violet" => "bg-violet-50 text-violet-700 ring-violet-200",
        "slate" => "bg-slate-100 text-slate-600 ring-slate-200",
        _ => "bg-indigo-50 text-indigo-700 ring-indigo-200"
    };

    public static string FileIcon(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "file-text",
        ".doc" or ".docx" => "file-type-2",
        ".ppt" or ".pptx" => "presentation",
        _ => "file"
    };

    public static string FileTone(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "text-rose-600 bg-rose-50 ring-rose-100",
        ".doc" or ".docx" => "text-blue-600 bg-blue-50 ring-blue-100",
        ".ppt" or ".pptx" => "text-amber-600 bg-amber-50 ring-amber-100",
        _ => "text-slate-600 bg-slate-50 ring-slate-200"
    };

    public static string TypeBadge(AcademicTaskType type) => type switch
    {
        AcademicTaskType.ExamPrep => "bg-violet-50 text-violet-700 ring-violet-200",
        AcademicTaskType.Assignment => "bg-indigo-50 text-indigo-700 ring-indigo-200",
        AcademicTaskType.Homework => "bg-sky-50 text-sky-700 ring-sky-200",
        AcademicTaskType.Reading => "bg-teal-50 text-teal-700 ring-teal-200",
        AcademicTaskType.Project => "bg-orange-50 text-orange-700 ring-orange-200",
        _ => "bg-slate-100 text-slate-600 ring-slate-200"
    };

    public static int Percent(int value, int total) =>
        total <= 0 ? 0 : (int)Math.Round(value * 100d / total);

    public static string StatTone(string tone) => tone switch
    {
        "emerald" => "bg-emerald-50 text-emerald-600 ring-emerald-100",
        "amber" => "bg-amber-50 text-amber-600 ring-amber-100",
        "rose" => "bg-rose-50 text-rose-600 ring-rose-100",
        "blue" => "bg-blue-50 text-blue-600 ring-blue-100",
        "violet" => "bg-violet-50 text-violet-600 ring-violet-100",
        "sky" => "bg-sky-50 text-sky-600 ring-sky-100",
        _ => "bg-brand-50 text-brand-600 ring-brand-100"
    };

    public static string ActivityTone(string tone) => tone switch
    {
        "emerald" => "bg-emerald-50 text-emerald-600",
        "rose" => "bg-rose-50 text-rose-600",
        "amber" => "bg-amber-50 text-amber-600",
        "sky" => "bg-sky-50 text-sky-600",
        "violet" => "bg-violet-50 text-violet-600",
        _ => "bg-indigo-50 text-indigo-600"
    };

    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "SF",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
        };
    }
}
