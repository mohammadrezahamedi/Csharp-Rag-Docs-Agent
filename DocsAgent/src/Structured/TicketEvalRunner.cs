namespace Structured;

/// <summary>Runs the extractor over test cases and prints a console table plus summary rates.</summary>
public sealed class TicketEvalRunner
{
    private readonly IStructuredExtractor<TicketInfo> _extractor;

    public TicketEvalRunner(IStructuredExtractor<TicketInfo> extractor)
    {
        _extractor = extractor;
    }

    public async Task RunAsync(IReadOnlyList<TicketEvalCase> cases, CancellationToken ct = default)
    {
        Console.WriteLine($"[Structured/Eval] Running {cases.Count} case(s)");

        int validCount = 0, correctFields = 0, totalRetries = 0;
        const int fieldsPerCase = 3; // category, urgency, mentionsRefund

        var rows = new List<string>();

        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            var result = await _extractor.ExtractAsync(c.Input, ct);

            totalRetries += result.Attempts - 1;
            string catOk = "-", urgOk = "-", refOk = "-";

            if (result.Success && result.Value != null)
            {
                validCount++;
                bool cat = Same(result.Value.Category, c.ExpectedCategory);
                bool urg = Same(result.Value.Urgency, c.ExpectedUrgency);
                bool refd = result.Value.MentionsRefund == c.ExpectedMentionsRefund;

                correctFields += (cat ? 1 : 0) + (urg ? 1 : 0) + (refd ? 1 : 0);
                catOk = cat ? "OK" : "WRONG";
                urgOk = urg ? "OK" : "WRONG";
                refOk = refd ? "OK" : "WRONG";
            }

            rows.Add($"{i + 1,-3}| {(result.Success ? "yes" : "NO"),-6}| {catOk,-7}| {urgOk,-7}| {refOk,-7}| {result.Attempts - 1}");
            Console.WriteLine($"[Structured/Eval] Case {i + 1} done. Success={result.Success}, Attempts={result.Attempts}");
        }

        Console.WriteLine();
        Console.WriteLine("#  | Valid | Cat    | Urg    | Refund | Retries");
        Console.WriteLine("---+-------+--------+--------+--------+--------");
        foreach (var row in rows) Console.WriteLine(row);
        Console.WriteLine();

        double validRate = 100.0 * validCount / cases.Count;
        double fieldRate = 100.0 * correctFields / (cases.Count * fieldsPerCase);

        Console.WriteLine($"[Structured/Eval] Valid JSON rate : {validCount}/{cases.Count} ({validRate:F0}%)");
        Console.WriteLine($"[Structured/Eval] Correct fields  : {correctFields}/{cases.Count * fieldsPerCase} ({fieldRate:F0}%)");
        Console.WriteLine($"[Structured/Eval] Retries used    : {totalRetries}");
    }

    private static bool Same(string? a, string? b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}