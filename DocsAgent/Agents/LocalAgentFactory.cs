using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DocsAgent.Agents;

public static class LocalAgentFactory
{
    private const string Tag = "[DocsAgent/Factory]";

    // Builds an agent on top of any IChatClient (Ollama, Azure, ...) and gives it the tools.
    public static AIAgent Create(
        IChatClient chatClient,
        string instructions,
        IList<AITool>? tools = null)
    {
        Console.WriteLine($"{Tag} Creating agent. Tools={tools?.Count ?? 0}");

        AIAgent agent = new ChatClientAgent(
            chatClient,
            instructions: instructions,
            name: "LocalChat",
            tools: tools);

        Console.WriteLine($"{Tag} Agent created.");
        return agent;
    }
}