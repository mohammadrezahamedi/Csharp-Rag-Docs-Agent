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
    private readonly ITracer _tracer;

    public AgentRunnerAdapter(AIAgent agent, ITracer? tracer = null)
    {
        _agent = agent;
        _tracer = tracer ?? new NullTracer();
    }

    public async Task<string> AskAsync(string question)
    {
        Console.WriteLine($"{Tag} Asking with a fresh history: '{question}'");

        return await _tracer.MeasureAsync("agent", "ask", question, async () =>
        {
            // Fresh history per call, so eval cases never influence each other.
            var history = new List<ChatMessage> { new ChatMessage(ChatRole.User, question) };
            var answer = new StringBuilder();

            await foreach (var update in _agent.RunStreamingAsync(history))
            {
                answer.Append(update.ToString());
            }

            return answer.ToString();
        });
    }
}
