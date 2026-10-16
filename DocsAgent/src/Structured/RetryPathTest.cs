namespace Structured;

/// <summary>Proves the retry loop works: recovers from bad replies, and fails cleanly when all attempts are bad.</summary>
public static class RetryPathTest
{
    public static async Task RunAsync()
    {
        // Scenario A: not JSON -> invalid category -> valid. Expect success on attempt 3.
        Console.WriteLine("[Structured/RetryTest] Scenario A: bad, bad, good");
        var clientA = new FakeChatClient(new[]
        {
            "this is not json",
            "{\"category\":\"refunds\",\"urgency\":\"high\",\"summary\":\"x\",\"mentionsRefund\":true}",
            "{\"category\":\"billing\",\"urgency\":\"high\",\"summary\":\"Double charge\",\"mentionsRefund\":true}"
        });
        var resultA = await new TicketExtractor(clientA).ExtractAsync("test input");
        Console.WriteLine($"[Structured/RetryTest] A: Success={resultA.Success} Attempts={resultA.Attempts} (expected True / 3)");

        // Scenario B: always bad. Expect a clean failure after 3 attempts, no infinite loop.
        Console.WriteLine("[Structured/RetryTest] Scenario B: bad, bad, bad");
        var clientB = new FakeChatClient(new[] { "nope", "still nope", "no again" });
        var resultB = await new TicketExtractor(clientB).ExtractAsync("test input");
        Console.WriteLine($"[Structured/RetryTest] B: Success={resultB.Success} Attempts={resultB.Attempts} (expected False / 3)");
        Console.WriteLine($"[Structured/RetryTest] B last error: {resultB.LastError}");
    }
}