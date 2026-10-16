using Planner;

namespace DocsAgent.Agents;

// Runs a few multi-step questions through the planner. Output is console only.
public static class PlannerEvalRunner
{
    private const string Tag = "[Planner/Eval]";

    private sealed record Case(string Question, string[] ExpectedAny, int MinSteps);

    private static readonly Case[] Cases =
    {
        new("Check ticket T-1002 and calculate the refund for an annual plan after 20 days.",
            new[] { "50", "half", "T-1002" }, 2),
        new("Compare what the refund doc and the FAQ say about refunds.",
            new[] { "refund" }, 2),
        new("Get the status of ticket T-1001 and tell me which docs exist.",
            new[] { "T-1001", "filenest" }, 2),
    };

    public static async Task RunAsync(IPlanner planner)
    {
        int passed = 0;
        for (int i = 0; i < Cases.Length; i++)
        {
            var c = Cases[i];
            Console.WriteLine($"{Tag} ===== Case {i + 1}/{Cases.Length}: {c.Question}");

            var result = await planner.RunAsync(c.Question);

            bool ok = result.Success
                      && result.Steps.Count >= c.MinSteps
                      && c.ExpectedAny.Any(k => result.FinalAnswer.Contains(k, StringComparison.OrdinalIgnoreCase));

            Console.WriteLine($"{Tag} Case {i + 1}: {(ok ? "PASS" : "FAIL")} (steps={result.Steps.Count}, success={result.Success}, error={result.Error ?? "none"})");
            if (ok) passed++;
        }

        Console.WriteLine($"{Tag} Result: {passed}/{Cases.Length}");
    }
}