namespace StudyFlow.Services;

/// <summary>Business-rule failure that should be shown to the student, not a 500 error.</summary>
public class StudyFlowValidationException : Exception
{
    public StudyFlowValidationException(string message) : base(message) { }
}
