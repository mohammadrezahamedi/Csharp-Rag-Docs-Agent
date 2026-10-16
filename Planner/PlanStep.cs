namespace Planner;

/// <summary>
/// One step of a plan created by the model.
/// ToolNeeded is null or "none" when the step needs no tool.
/// </summary>
public sealed record PlanStep(
    int Number,
    string Description,
    string? ToolNeeded);