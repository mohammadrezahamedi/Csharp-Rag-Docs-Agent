using Microsoft.Extensions.AI;

namespace DocsAgent.Memory;

// A conversation history that decides by itself what to keep.
public interface IChatMemory
{
    int Count { get; }
    void Add(ChatMessage message);
    void RemoveLast();
    IReadOnlyList<ChatMessage> GetHistory();
    void Clear();
}