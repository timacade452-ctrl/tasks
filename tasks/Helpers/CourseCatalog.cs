namespace StudyFlow.Helpers;

public record ColorOption(string Name, string Hex);
public record IconOption(string Name, string Label);

public static class CourseCatalog
{
    public static readonly IReadOnlyList<ColorOption> Colors =
    [
        new("Indigo", "#4f46e5"),
        new("Blue", "#2563eb"),
        new("Sky", "#0284c7"),
        new("Teal", "#0d9488"),
        new("Emerald", "#059669"),
        new("Amber", "#d97706"),
        new("Rose", "#e11d48"),
        new("Violet", "#7c3aed"),
        new("Slate", "#475569")
    ];

    public static readonly IReadOnlyList<IconOption> Icons =
    [
        new("book-open", "Book"),
        new("code-2", "Code"),
        new("sigma", "Math"),
        new("atom", "Science"),
        new("flask-conical", "Lab"),
        new("globe-2", "Humanities"),
        new("landmark", "Business"),
        new("pen-tool", "Design"),
        new("languages", "Language"),
        new("cpu", "Technology"),
        new("chart-line", "Statistics"),
        new("microscope", "Research")
    ];

    public static string HexFor(string? color) =>
        Colors.FirstOrDefault(c => string.Equals(c.Hex, color, StringComparison.OrdinalIgnoreCase))?.Hex
        ?? "#4f46e5";

    public static string NameFor(string? color) =>
        Colors.FirstOrDefault(c => string.Equals(c.Hex, color, StringComparison.OrdinalIgnoreCase))?.Name
        ?? "Indigo";

    public static bool IsValidColor(string? color) =>
        Colors.Any(c => string.Equals(c.Hex, color, StringComparison.OrdinalIgnoreCase));

    public static bool IsValidIcon(string? icon) =>
        Icons.Any(i => string.Equals(i.Name, icon, StringComparison.OrdinalIgnoreCase));
}
