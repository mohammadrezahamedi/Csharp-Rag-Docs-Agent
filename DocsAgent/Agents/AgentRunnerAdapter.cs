using System.Text;
using DocsAgent.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DocsAgent.Agents;

// Wraps the real agent so other modules only see IAgentRunner.
public sealed class AgentRunnerAdapter : IAgentRunner
{
    private const string Tag = "[Agents/Adapter]";

    private readonly AIAgent _agent;

    public AgentRunnerAdapter(AIAgent agent)
    {
        _agent = agent;
    }

    public async Task<string> AskAsync(string question)
    {
        Console.WriteLine($"{Tag} Asking with a fresh history: '{question}'");

        // Fresh history per call, so eval cases never influence each other.
        var history = new List<ChatMessage> { new ChatMessage(ChatRole.User, question) };
        var answer = new StringBuilder();

        await foreach (var update in _agent.RunStreamingAsync(history))
        {
            answer.Append(update.ToString());
        }

        return answer.ToString();
    }
}