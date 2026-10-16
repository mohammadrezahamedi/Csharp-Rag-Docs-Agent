using Microsoft.Extensions.AI;

namespace DocsAgent.Memory;

// Remembers nothing: every question is sent alone, without any earlier messages.
public sealed class NoMemory : IChatMemory
{
    private const string Tag = "[Memory/None]";

    private ChatMessage? _current;

    public int Count => _current == null ? 0 : 1;

    public void Add(ChatMessage message)
    {
        // Only the current user question is kept; assistant replies are forgotten at once.
        if (message.Role == ChatRole.User)
        {
            _current = message;
            Console.WriteLine($"{Tag} Holding only the current question");
        }
        else
        {
            Console.WriteLine($"{Tag} Ignored {message.Role} message (no memory)");
        }
    }

    public void RemoveLast()
    {
        _current = null;
        Console.WriteLine($"{Tag} Removed current question");
    }

    public IReadOnlyList<ChatMessage> GetHistory()
    {
        return _current == null ? Array.Empty<ChatMessage>() : new[] { _current };
    }

    public void Clear()
    {
        _current = null;
        Console.WriteLine($"{Tag} Cleared.");
    }
}