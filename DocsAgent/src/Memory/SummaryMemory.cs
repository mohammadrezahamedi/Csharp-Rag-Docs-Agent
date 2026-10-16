using System.Text;
using Microsoft.Extensions.AI;

namespace DocsAgent.Memory;

// Keeps recent messages as they are. When the history gets long, older messages
// are squeezed into one short summary that stays at the top of the history.
public sealed class SummaryMemory : IChatMemory
{
    private const string Tag = "[Memory/Summary]";

    private const string SummarizerPrompt =
        "You summarize conversations. Write a short summary (max 5 sentences) of the conversation below. " +
        "Keep names, numbers and decisions. Reply with the summary only.";

    private readonly IChatClient _chatClient;
    private readonly int _maxMessages;
    private readonly int _keepRecent;
    private readonly List<ChatMessage> _messages = new();
    private string _summary = string.Empty;

    public SummaryMemory(IChatClient chatClient, int maxMessages = 12, int keepRecent = 4)
    {
        if (keepRecent < 2 || maxMessages <= keepRecent)
        {
            throw new ArgumentException("Need keepRecent >= 2 and maxMessages > keepRecent");
        }

        _chatClient = chatClient;
        _maxMessages = maxMessages;
        _keepRecent = keepRecent;
        Console.WriteLine($"{Tag} Created. maxMessages={_maxMessages}, keepRecent={_keepRecent}");
    }

    public int Count => _messages.Count;

    public void Add(ChatMessage message)
    {
        _messages.Add(message);
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

    // The summary (if any) goes first as a system message, then the recent messages.
    public IReadOnlyList<ChatMessage> GetHistory()
    {
        var history = new List<ChatMessage>();

        if (_summary.Length > 0)
        {
            history.Add(new ChatMessage(ChatRole.System, $"Summary of the earlier conversation: {_summary}"));
        }

        history.AddRange(_messages);
        return history;
    }

    public void Clear()
    {
        _messages.Clear();
        _summary = string.Empty;
        Console.WriteLine($"{Tag} Cleared.");
    }

    public async Task CompactAsync()
    {
        if (_messages.Count <= _maxMessages)
        {
            return;
        }

        // Cut so the recent part starts with a user message.
        int cut = _messages.Count - _keepRecent;
        while (cut < _messages.Count && _messages[cut].Role != ChatRole.User)
        {
            cut++;
        }

        if (cut <= 0 || cut >= _messages.Count)
        {
            Console.WriteLine($"{Tag} No safe cut point, skipping");
            return;
        }

        var older = _messages.GetRange(0, cut);
        Console.WriteLine($"{Tag} Summarizing {older.Count} old message(s)...");

        try
        {
            _summary = await SummarizeAsync(older);
            _messages.RemoveRange(0, cut);
            Console.WriteLine($"{Tag} Summary updated ({_summary.Length} chars). Messages kept = {_messages.Count}");
            Console.WriteLine($"{Tag} SUMMARY: {_summary}");
        }
        catch (Exception ex)
        {
            // On failure keep everything; we will try again next turn.
            Console.WriteLine($"{Tag} ERROR while summarizing: {ex.Message}. Keeping all messages.");
        }
    }

    private async Task<string> SummarizeAsync(List<ChatMessage> older)
    {
        var text = new StringBuilder();

        if (_summary.Length > 0)
        {
            text.AppendLine($"Earlier summary: {_summary}");
            text.AppendLine();
        }

        foreach (ChatMessage message in older)
        {
            text.AppendLine($"{message.Role}: {message.Text}");
        }

        var request = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, SummarizerPrompt),
            new ChatMessage(ChatRole.User, text.ToString())
        };

        ChatResponse response = await _chatClient.GetResponseAsync(request);
        return response.Text.Trim();
    }
}