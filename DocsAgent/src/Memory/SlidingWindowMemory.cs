using Microsoft.Extensions.AI;

namespace DocsAgent.Memory;

// Keeps only the last N messages. Older messages are dropped.
public sealed class SlidingWindowMemory : IChatMemory
{
    private const string Tag = "[Memory/Window]";

    private readonly int _maxMessages;
    private readonly List<ChatMessage> _messages = new();

    public SlidingWindowMemory(int maxMessages = 20)
    {
        if (maxMessages < 2)
        {
            throw new ArgumentException("maxMessages must be at least 2");
        }

        _maxMessages = maxMessages;
        Console.WriteLine($"{Tag} Created. maxMessages={_maxMessages}");
    }

    public int Count => _messages.Count;

    public void Add(ChatMessage message)
    {
        _messages.Add(message);
        Trim();
        Console.WriteLine($"{Tag} Added {message.Role}. Messages kept = {_messages.Count}");
    }

    public void RemoveLast()
    {
        if (_messages.Count > 0)
        {
            _messages.RemoveAt(_messages.Count - 1);
            Console.WriteLine($"{Tag} Removed last. Messages kept = {_messages.Count}");
        }
    }

    // Returns a copy, so callers cannot change our list by accident.
    public IReadOnlyList<ChatMessage> GetHistory()
    {
        return _messages.ToList();
    }

    public void Clear()
    {
        _messages.Clear();
        Console.WriteLine($"{Tag} Cleared.");
    }

    private void Trim()
    {
        int dropped = 0;

        while (_messages.Count > _maxMessages)
        {
            _messages.RemoveAt(0);
            dropped++;
        }

        // The history must start with a user message, never with a lonely assistant reply.
        while (_messages.Count > 0 && _messages[0].Role != ChatRole.User)
        {
            _messages.RemoveAt(0);
            dropped++;
        }

        if (dropped > 0)
        {
            Console.WriteLine($"{Tag} Dropped {dropped} old message(s)");
        }
    }
}