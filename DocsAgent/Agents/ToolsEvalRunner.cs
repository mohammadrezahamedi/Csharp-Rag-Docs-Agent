using DocsAgent.Shared;

namespace DocsAgent.Agents;

// Small eval for tool choice. Tool calls are visible in the [Tools/Tickets] logs.
public static class ToolsEvalRunner
{
    private const string Tag = "[Agents/ToolsEval]";

    private sealed record Case(string Name, string Question, string[] AnyOfExpected);

    private static readonly Case[] Cases =
    {
    new("Ticket status", "What is the status of ticket T-1002?",
        new[] { "in_progress", "in progress" }),
    new("Refund annual 10 days", "How much refund do I get for an annual plan after 10 days?",
        new[] { "100" }),
    new("Refund monthly 20 days", "How much refund do I get for a monthly plan after 20 days?",
        new[]
        {
            "0 usd", "$0", "no refund", "not eligible",
            "not get any refund", "don't get any refund", "do not get any refund",
            "not receive any refund", "not receive a refund", "not receive refund",
            "will not get", "won't get"
        }),
    new("Wrong ticket id", "What is the status of ticket T-9999?",
        new[] { "not found", "couldn't find", "could not find", "doesn't exist", "does not exist" }),
    new("No tool needed", "Hello!",
        Array.Empty<string>()),
};
    private static void PrintColored(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static async Task RunAsync(IAgentRunner runner)
    {
        int pass = 0;

        foreach (var c in Cases)
        {
            Console.WriteLine($"\n{Tag} ---- {c.Name}: '{c.Question}'");

            string answer;
            try
            {
                answer = await runner.AskAsync(c.Question);
            }
            catch (Exception ex)
            {
                PrintColored($"{Tag} FAIL (exception): {ex.Message}", ConsoleColor.Red);
                continue;
            }

            // Green = model answer, so it stands out from code logs.
            PrintColored($"{Tag} Answer: {answer}", ConsoleColor.Green);

            bool ok = c.AnyOfExpected.Length == 0
                || c.AnyOfExpected.Any(e => answer.Contains(e, StringComparison.OrdinalIgnoreCase));

            if (ok) pass++;
            PrintColored($"{Tag} {(ok ? "PASS" : "FAIL")}", ok ? ConsoleColor.Green : ConsoleColor.Red);
        }

        PrintColored($"\n{Tag} Result: {pass}/{Cases.Length}", pass == Cases.Length ? ConsoleColor.Green : ConsoleColor.Yellow);
        Console.WriteLine($"{Tag} Check the [Tools/Tickets] logs above to see which tool was really called.");
    }
}