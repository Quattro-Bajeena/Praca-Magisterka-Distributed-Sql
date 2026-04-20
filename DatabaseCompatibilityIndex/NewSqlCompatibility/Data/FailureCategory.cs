namespace NSCI.Data;

/// <summary>
/// Classifies the type of failure observed when a test does not pass.
/// Stored as an integer in the database. Null means the failure has not been classified yet.
/// </summary>
public enum FailureCategory
{
    /// <summary>
    /// The database returned an explicit "feature not supported" message.
    /// Best outcome — the database clearly communicates the limitation.
    /// </summary>
    UnsupportedFeatureExplicit = 1,

    /// <summary>
    /// The database returned an "unrecognized command" or syntax error.
    /// Acceptable — implies the syntax is simply not implemented.
    /// </summary>
    UnrecognizedSyntax = 2,

    /// <summary>
    /// The database returned a vague, unhelpful error message with little diagnostic value.
    /// </summary>
    VagueError = 3,

    /// <summary>
    /// The query executed without error but returned a different result than the baseline database.
    /// Worst outcome — silent incompatibility that is hard to detect.
    /// </summary>
    WrongResult = 4,

    /// <summary>
    /// The failure appears to be fixable (e.g., via configuration, extension, or workaround),
    /// rather than a fundamental limitation of the database.
    /// </summary>
    Fixable = 5,
}
