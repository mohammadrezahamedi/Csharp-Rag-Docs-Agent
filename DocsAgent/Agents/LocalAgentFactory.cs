using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace DocsAgent.Agents;

public static class LocalAgentFactory
{
    private const string Tag = "[DocsAgent/Factory]";

    // Builds an agent that talks to a local Ollama server and can call the given tools.
    public static AIAgent Create(
        string endpoint,
        string modelName,
        string instructions,
        IList<AITool>? tools = null)
    {
        Console.WriteLine($"{Tag} Creating agent. Endpoint={endpoint}, Model={modelName}, Tools={tools?.Count ?? 0}");

        // OllamaApiClient implements IChatClient, so it can be wrapped by ChatClientAgent.
        IChatClient chatClient = new OllamaApiClient(new Uri(endpoint), modelName);
        Console.WriteLine($"{Tag} Chat client created.");

        AIAgent agent = new ChatClientAgent(
            chatClient,
            instructions: instructions,
            name: "LocalChat",
            tools: tools);

        Console.WriteLine($"{Tag} Agent created.");
        return agent;
    }
}