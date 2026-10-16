namespace Structured;

/// <summary>Outcome of one extraction run, including how many attempts were used.</summary>
public sealed record ExtractionResult<T>(
    bool Success,
    T? Value,
    int Attempts,
    string? LastError,
    string? LastRawOutput);