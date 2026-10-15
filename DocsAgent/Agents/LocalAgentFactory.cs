using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace DocsAgent.Agents;

public static class LocalAgentFactory
{
    private const string Tag = "[DocsAgent/Factory]";

    // Builds an agent that talks to a local Ollama server (no internet needed after model download).
    public static AIAgent Create(string endpoint, string modelName, string instructions)
    {
        Console.WriteLine($"{Tag} Creating agent. Endpoint={endpoint}, Model={modelName}");

        // OllamaApiClient implements IChatClient, so it can be wrapped by ChatClientAgent.
        IChatClient chatClient = new OllamaApiClient(new Uri(endpoint), modelName);
        Console.WriteLine($"{Tag} Chat client created.");

        AIAgent agent = new ChatClientAgent(
            chatClient,
            instructions: instructions,
            name: "LocalChat");

        Console.WriteLine($"{Tag} Agent created.");
        return agent;
    }
}