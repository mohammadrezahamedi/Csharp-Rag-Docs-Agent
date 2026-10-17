namespace Structured;

/// <summary>Runs the extractor N times per test case (stochastic eval) and prints pass rates per case and overall.</summary>
public sealed class TicketEvalRunner
{
    private readonly IStructuredExtractor<TicketInfo> _extractor;

    public TicketEvalRunner(IStructuredExtractor<TicketInfo> extractor)
    {
        _extractor = extractor;
    }

    public async Task RunAsync(IReadOnlyList<TicketEvalCase> cases, CancellationToken ct = default, int runsPerCase = 5)
    {
        Console.WriteLine($"[Structured/Eval] Running {cases.Count} case(s) x {runsPerCase} run(s) each");

        int totalRuns = 0, totalValid = 0, totalPerfect = 0, totalRetries = 0;
        int totalCat = 0, totalUrg = 0, totalRef = 0;
        var rows = new List<(string Line, double PerfectRate)>();

        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            int valid = 0, perfect = 0, catOk = 0, urgOk = 0, refOk = 0, retries = 0;

            for (int r = 0; r < runsPerCase; r++)
            {
                var result = await _extractor.ExtractAsync(c.Input, ct);
                retries += result.Attempts - 1;

                if (result.Success && result.Value != null)
                {
                    valid++;
                    bool cat = Same(result.Value.Category, c.ExpectedCategory);
                    bool urg = Same(result.Value.Urgency, c.ExpectedUrgency);
                    bool refd = result.Value.MentionsRefund == c.ExpectedMentionsRefund;

                    if (cat) catOk++;
                    if (urg) urgOk++;
                    if (refd) refOk++;
                    if (cat && urg && refd) perfect++;

                    // Model answer is the important log: cyan when all fields match, yellow when not
                    Console.ForegroundColor = (cat && urg && refd) ? ConsoleColor.Cyan : ConsoleColor.Yellow;
                    Console.WriteLine($"[Model] case {i + 1} run {r + 1}: cat={result.Value.Category} urg={result.Value.Urgency} refund={result.Value.MentionsRefund} (expected {c.ExpectedCategory}/{c.ExpectedUrgency}/{c.ExpectedMentionsRefund})");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Model] case {i + 1} run {r + 1}: INVALID output after {result.Attempts} attempt(s)");
                    Console.ResetColor();
                }
            }

            totalRuns += runsPerCase; totalValid += valid; totalPerfect += perfect; totalRetries += retries;
            totalCat += catOk; totalUrg += urgOk; totalRef += refOk;

            double perfectRate = 100.0 * perfect / runsPerCase;
            string flaky = (perfect > 0 && perfect < runsPerCase) ? "  <- flaky" : "";
            string line = $"{i + 1,-3}| {Pct(valid, runsPerCase),-6}| {Pct(perfect, runsPerCase),-8}| {Pct(catOk, runsPerCase),-5}| {Pct(urgOk, runsPerCase),-5}| {Pct(refOk, runsPerCase),-7}| {retries}{flaky}";
            rows.Add((line, perfectRate));
            Console.WriteLine($"[Structured/Eval] Case {i + 1} done. Perfect {perfect}/{runsPerCase}");
        }

        Console.WriteLine();
        Console.WriteLine("#  | Valid | Perfect | Cat | Urg | Refund | Retries");
        Console.WriteLine("---+-------+---------+-----+-----+--------+--------");
        foreach (var (line, rate) in rows) PrintByRate(line, rate);
        Console.WriteLine();

        double validRate = 100.0 * totalValid / totalRuns;
        double perfectOverall = 100.0 * totalPerfect / totalRuns;
        double fieldRate = 100.0 * (totalCat + totalUrg + totalRef) / (totalRuns * 3);

        PrintByRate($"[Structured/Eval] Valid JSON rate : {totalValid}/{totalRuns} ({validRate:F0}%)", validRate);
        PrintByRate($"[Structured/Eval] Perfect runs    : {totalPerfect}/{totalRuns} ({perfectOverall:F0}%)", perfectOverall);
        PrintByRate($"[Structured/Eval] Correct fields  : {totalCat + totalUrg + totalRef}/{totalRuns * 3} ({fieldRate:F0}%)", fieldRate);
        Console.WriteLine($"[Structured/Eval] Retries used    : {totalRetries}");
    }

    private static string Pct(int part, int total) => $"{100 * part / total}%";

    // Green >= 80, yellow >= 50, red below
    private static void PrintByRate(string text, double rate)
    {
        Console.ForegroundColor = rate >= 80 ? ConsoleColor.Green : rate >= 50 ? ConsoleColor.Yellow : ConsoleColor.Red;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    private static bool Same(string? a, string? b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}
