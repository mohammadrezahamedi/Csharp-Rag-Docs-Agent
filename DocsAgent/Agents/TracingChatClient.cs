using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using DocsAgent.Shared;
using Microsoft.Extensions.AI;

namespace DocsAgent.Agents;

// Wraps any IChatClient and records each model call (input, answer, requested tool calls, time).
public sealed class TracingChatClient : DelegatingChatClient
{
    private readonly ITracer _tracer;

    public TracingChatClient(IChatClient inner, ITracer tracer) : base(inner)
    {
        _tracer = tracer;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        return await _tracer.MeasureAsync(
            "model", "chat", LastUser(list),
            () => base.GetResponseAsync(list, options, cancellationToken),
            r => Describe(r.Text, r.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Select(c => c.Name)));
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        var sw = Stopwatch.StartNew();
        var text = new StringBuilder();
        var calls = new List<string>();

        await foreach (var update in base.GetStreamingResponseAsync(list, options, cancellationToken))
        {
            text.Append(update.Text);
            foreach (var c in update.Contents)
            {
                if (c is FunctionCallContent fc) calls.Add(fc.Name);
            }
            yield return update;
        }

        _tracer.Record("model", "chat-stream", LastUser(list), Describe(text.ToString(), calls), sw.ElapsedMilliseconds);
    }

    private static string? LastUser(List<ChatMessage> list) =>
        list.LastOrDefault(m => m.Role == ChatRole.User)?.Text;

    private static string Describe(string text, IEnumerable<string> toolCalls)
    {
        var names = toolCalls.ToList();
        return names.Count > 0 ? $"[tool calls: {string.Join(",", names)}] {text}" : text;
    }
}
