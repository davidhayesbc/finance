namespace Privestio.Domain.Enums;

/// <summary>
/// Controls import behavior when errors are encountered.
/// </summary>
public enum ImportPolicy
{
    /// <summary>
    /// Skip invalid rows and continue importing valid ones (default).
    /// </summary>
    SkipInvalid = 0,

    /// <summary>
    /// Abort the entire import on the first error, importing nothing.
    /// </summary>
    FailFast = 1,

    /// <summary>
    /// Parse and validate only; do not persist any transactions.
    /// </summary>
    PreviewOnly = 2,
}
