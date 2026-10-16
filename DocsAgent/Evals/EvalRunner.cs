using System.Diagnostics;
using System.Text;
using DocsAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DocsAgent.Evals;

public sealed class EvalRunner
{
    private const string Tag = "[Evals/Runner]";

    // If the model says one of these, we treat it as an honest "I could not find it".
    private static readonly string[] RefusalPhrases =
    {
        "could not find", "couldn't find", "could not locate", "not mentioned",
        "not found", "no information", "does not contain", "doesn't contain",
        "not in the document", "not specified", "no mention", "not provided"
    };

    private readonly AIAgent _agent;
    private readonly IReadOnlyList<IToolCallSource> _toolSources;
    private readonly List<string> _currentCalls = new();

    public EvalRunner(AIAgent agent, IReadOnlyList<IToolCallSource> toolSources)
    {
        _agent = agent;
        _toolSources = toolSources;
    }

    public async Task RunAsync(IReadOnlyList<EvalCase> cases)
    {
        Console.WriteLine($"{Tag} Starting eval. Cases = {cases.Count}");

        var totals = new Dictionary<EvalKind, int>();
        var passes = new Dictionary<EvalKind, int>();
        int passed = 0;

        foreach (IToolCallSource source in _toolSources)
        {
            source.ToolCalled += OnToolCalled;
        }

        try
        {
            foreach (EvalCase evalCase in cases)
            {
                bool ok = await RunCaseAsync(evalCase);

                totals[evalCase.Kind] = totals.GetValueOrDefault(evalCase.Kind) + 1;
                if (ok)
                {
                    passed++;
                    passes[evalCase.Kind] = passes.GetValueOrDefault(evalCase.Kind) + 1;
                }
            }
        }
        finally
        {
            foreach (IToolCallSource source in _toolSources)
            {
                source.ToolCalled -= OnToolCalled;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{Tag} ================ SUMMARY ================");
        foreach (var kv in totals)
        {
            Console.WriteLine($"{Tag} {kv.Key}: {passes.GetValueOrDefault(kv.Key)}/{kv.Value}");
        }
        Console.WriteLine($"{Tag} TOTAL: {passed}/{cases.Count}");
        Console.WriteLine($"{Tag} =========================================");
    }

    private void OnToolCalled(string toolName)
    {
        _currentCalls.Add(toolName);
    }

    private async Task<bool> RunCaseAsync(EvalCase evalCase)
    {
        _currentCalls.Clear();

        Console.WriteLine();
        Console.WriteLine($"{Tag} --- Case '{evalCase.Id}' ({evalCase.Kind}) ---");
        Console.WriteLine($"{Tag} Question: {evalCase.Question}");

        // Fresh history for every case, so cases never influence each other.
        var history = new List<ChatMessage> { new ChatMessage(ChatRole.User, evalCase.Question) };
        var answer = new StringBuilder();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await foreach (var update in _agent.RunStreamingAsync(history))
            {
                answer.Append(update.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{Tag} ERROR: {ex.Message}");
            Console.WriteLine($"{Tag} RESULT: FAIL (exception)");
            return false;
        }

        stopwatch.Stop();

        string answerText = answer.ToString();
        Console.WriteLine($"{Tag} Answer: {answerText}");
        Console.WriteLine($"{Tag} Tool calls: [{string.Join(", ", _currentCalls)}], time = {stopwatch.ElapsedMilliseconds} ms");

        bool ok = Judge(evalCase, answerText, out string reason);
        Console.WriteLine($"{Tag} RESULT: {(ok ? "PASS" : "FAIL")} ({reason})");
        return ok;
    }

    private bool Judge(EvalCase evalCase, string answer, out string reason)
    {
        string normalized = answer.ToLowerInvariant().Replace('\u2019', '\'');
        bool readDocCalled = _currentCalls.Contains("ReadDoc") || _currentCalls.Contains("SearchDocs");

        switch (evalCase.Kind)
        {
            case EvalKind.Answerable:
                if (!readDocCalled)
                {
                    reason = "no document tool was called (model may have guessed)";
                    return false;
                }

                foreach (string needle in evalCase.MustContainAll ?? Array.Empty<string>())
                {
                    if (!normalized.Contains(needle.ToLowerInvariant()))
                    {
                        reason = $"answer is missing '{needle}'";
                        return false;
                    }
                }

                reason = "tool called and all required words found";
                return true;

            case EvalKind.Unanswerable:
                foreach (string phrase in RefusalPhrases)
                {
                    if (normalized.Contains(phrase))
                    {
                        reason = $"model admitted it could not find it ('{phrase}')";
                        return true;
                    }
                }

                reason = "model did not admit the answer is missing (possible hallucination)";
                return false;

            case EvalKind.NoToolNeeded:
                if (readDocCalled)
                {
                    reason = "a document tool was called for small talk";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(answer))
                {
                    reason = "empty answer";
                    return false;
                }

                reason = "no document read and a reply was given";
                return true;

            default:
                reason = "unknown case kind";
                return false;
        }
    }
}