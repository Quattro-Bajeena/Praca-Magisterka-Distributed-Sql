namespace NSCI.Testing;

public record TestResult(
    string TestName,
    string ClassName,
    SqlFeatureCategory Category,
    string Description,
    bool Passed,
    string? ErrorMessage,
    TimeSpan Duration);
