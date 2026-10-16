namespace Planner;

/// <summary>
/// Entry point of the planner module. Program.cs should only see this interface.
/// </summary>
public interface IPlanner
{
    Task<PlanResult> RunAsync(string question, CancellationToken ct = default);
}