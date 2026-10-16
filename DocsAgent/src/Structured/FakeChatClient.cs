using Microsoft.Extensions.AI;

namespace Structured;

/// <summary>Test double: returns scripted replies in order, ignoring the prompt.</summary>
public sealed class FakeChatClient : IChatClient
{
    private readonly Queue<string> _replies;

    public FakeChatClient(IEnumerable<string> replies)
    {
        _replies = new Queue<string>(replies);
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        string reply = _replies.Count > 0 ? _replies.Dequeue() : "";
        Console.WriteLine($"[Structured/Fake] Returning scripted reply: {reply}");
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Streaming is not used in this test double.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}