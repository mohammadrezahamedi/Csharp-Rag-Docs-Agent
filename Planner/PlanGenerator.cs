using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Planner;

public sealed class PlanGenerator : IPlanGenerator
{
    private readonly IChatClient _client;
    private readonly IReadOnlyList<string> _toolNames;
    private readonly int _maxSteps;
    private readonly int _maxAttempts;

    private sealed record RawStep(int Number, string? Description, string? Tool);

    public PlanGenerator(
        IChatClient client,
        IReadOnlyList<string> toolNames,
        int maxSteps = 5,
        int maxAttempts = 3)
    {
        _client = client;
        _toolNames = toolNames;
        _maxSteps = maxSteps;
        _maxAttempts = maxAttempts;
    }

    public async Task<PlanGenResult> GenerateAsync(string question, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, BuildSystemPrompt()),
            new(ChatRole.User, question)
        };

        string lastError = "unknown";

        for (int attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            Console.WriteLine($"[Planner/Gen] Attempt {attempt}/{_maxAttempts}");

            var response = await _client.GetResponseAsync(messages, cancellationToken: ct);
            var text = response.Text ?? "";
            Console.WriteLine($"[Planner/Gen] Raw model output: {text}");

            var (steps, error) = TryParseAndValidate(text);
            if (steps != null)
            {
                Console.WriteLine($"[Planner/Gen] Plan OK with {steps.Count} steps");
                foreach (var s in steps)
                    Console.WriteLine($"[Planner/Gen]   {s.Number}. {s.Description} (tool: {s.ToolNeeded ?? "none"})");
                return new PlanGenResult(true, steps, null);
            }

            lastError = error!;
            Console.WriteLine($"[Planner/Gen] Invalid plan: {lastError}");

            // Feed the error back so the model can fix its own output.
            messages.Add(new ChatMessage(ChatRole.Assistant, text));
            messages.Add(new ChatMessage(ChatRole.User,
                $"Your plan was invalid: {lastError}. Return ONLY the corrected JSON array."));
        }

        Console.WriteLine("[Planner/Gen] Giving up, plan invalid after all attempts");
        return new PlanGenResult(false, Array.Empty<PlanStep>(), lastError);
    }

    private string BuildSystemPrompt()
    {
        var tools = string.Join(", ", _toolNames);
        return
            "You are a planner. Break the user's question into at most " + _maxSteps + " short steps.\n" +
            "Return ONLY a JSON array, no extra text. Each item: " +
            "{\"number\": 1, \"description\": \"...\", \"tool\": \"ToolName or none\"}.\n" +
            "Available tools: " + tools + ".";
    }

    private (List<PlanStep>? Steps, string? Error) TryParseAndValidate(string text)
    {
        // Cut the JSON array out of the text (model may add code fences or chatter).
        int start = text.IndexOf('[');
        int end = text.LastIndexOf(']');
        if (start < 0 || end <= start)
            return (null, "no JSON array found");

        List<RawStep>? raw;
        try
        {
            raw = JsonSerializer.Deserialize<List<RawStep>>(
                text.Substring(start, end - start + 1),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            return (null, "JSON parse error: " + ex.Message);
        }

        if (raw == null || raw.Count == 0)
            return (null, "plan is empty");
        if (raw.Count > _maxSteps)
            return (null, $"too many steps ({raw.Count}), max is {_maxSteps}");

        var steps = new List<PlanStep>();
        for (int i = 0; i < raw.Count; i++)
        {
            var r = raw[i];
            if (string.IsNullOrWhiteSpace(r.Description))
                return (null, $"step {i + 1} has no description");

            string? tool = r.Tool;
            if (string.IsNullOrWhiteSpace(tool) || tool.Equals("none", StringComparison.OrdinalIgnoreCase))
                tool = null;
            else if (!_toolNames.Contains(tool, StringComparer.OrdinalIgnoreCase))
                return (null, $"step {i + 1} uses unknown tool '{tool}'");

            // Renumber ourselves so model numbering mistakes don't matter.
            steps.Add(new PlanStep(i + 1, r.Description!, tool));
        }

        return (steps, null);
    }
}