namespace Planner;

public sealed record PlanGenResult(
    bool Success,
    IReadOnlyList<PlanStep> Steps,
    string? Error);

/// <summary>
/// Turns a user question into a validated list of plan steps.
/// </summary>
public interface IPlanGenerator
{
    Task<PlanGenResult> GenerateAsync(string question, CancellationToken ct = default);
}