namespace Planner;

/// <summary>
/// Final result of a planner run: the plan, the per-step outputs and the final answer.
/// </summary>
public sealed record PlanResult(
    bool Success,
    IReadOnlyList<PlanStep> Steps,
    IReadOnlyList<string> StepOutputs,
    string FinalAnswer,
    string? Error);