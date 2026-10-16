using Microsoft.Extensions.AI;

namespace Structured;

/// <summary>Extracts a TicketInfo from text. Retries with the validation error fed back to the model.</summary>
public sealed class TicketExtractor : IStructuredExtractor<TicketInfo>
{
    private static readonly string[] AllowedCategories = { "billing", "technical", "account", "other" };
    private static readonly string[] AllowedUrgencies = { "low", "medium", "high" };

    private const string SystemPrompt =
        "You extract structured data from a support ticket. " +
        "Fields: category (billing|technical|account|other), urgency (low|medium|high), " +
        "summary (max 200 chars), mentionsRefund (true|false). " +
        "Return ONLY valid JSON, no extra text.";

    private readonly IChatClient _client;
    private readonly int _maxAttempts;

    public TicketExtractor(IChatClient client, int maxAttempts = 3)
    {
        _client = client;
        _maxAttempts = maxAttempts;
    }

    public async Task<ExtractionResult<TicketInfo>> ExtractAsync(string input, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, input)
        };

        string? lastError = null;
        string? lastRaw = null;

        for (int attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            Console.WriteLine($"[Structured/Extractor] Attempt {attempt}/{_maxAttempts}");

            var response = await _client.GetResponseAsync<TicketInfo>(messages, cancellationToken: ct);
            lastRaw = response.Text;
            Console.WriteLine($"[Structured/Extractor] Raw output: {lastRaw}");

            if (!response.TryGetResult(out var ticket))
            {
                lastError = "Output is not valid JSON matching the expected schema.";
            }
            else
            {
                lastError = Validate(ticket);
                if (lastError == null)
                {
                    Console.WriteLine($"[Structured/Extractor] Success on attempt {attempt}");
                    return new ExtractionResult<TicketInfo>(true, ticket, attempt, null, lastRaw);
                }
            }

            Console.WriteLine($"[Structured/Extractor] Invalid: {lastError}");

            // Feed the error back so the model can correct itself
            messages.Add(new ChatMessage(ChatRole.Assistant, lastRaw ?? ""));
            messages.Add(new ChatMessage(ChatRole.User,
                $"Your output was invalid: {lastError} Return ONLY the corrected JSON."));
        }

        Console.WriteLine("[Structured/Extractor] Failed after max attempts");
        return new ExtractionResult<TicketInfo>(false, null, _maxAttempts, lastError, lastRaw);
    }

    /// <summary>Returns an error message, or null if the ticket is valid.</summary>
    private static string? Validate(TicketInfo t)
    {
        if (!AllowedCategories.Contains(t.Category?.Trim().ToLowerInvariant()))
            return $"category must be one of: {string.Join(", ", AllowedCategories)}.";
        if (!AllowedUrgencies.Contains(t.Urgency?.Trim().ToLowerInvariant()))
            return $"urgency must be one of: {string.Join(", ", AllowedUrgencies)}.";
        if (string.IsNullOrWhiteSpace(t.Summary) || t.Summary.Length > 200)
            return "summary must be non-empty and at most 200 characters.";
        return null;
    }
}