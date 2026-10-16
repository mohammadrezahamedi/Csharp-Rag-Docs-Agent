using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DocsAgent.Agents;

public static class LocalAgentFactory
{
    private const string Tag = "[DocsAgent/Factory]";

    // Hard cap on model <-> tool round trips per question (loop guard).
    private const int MaxToolIterations = 3;

    // Builds an agent on top of any IChatClient (Ollama, Azure, ...) and gives it the tools.
    public static AIAgent Create(
        IChatClient chatClient,
        string instructions,
        IList<AITool>? tools = null)
    {
        Console.WriteLine($"{Tag} Creating agent. Tools={tools?.Count ?? 0}, MaxToolIterations={MaxToolIterations}");

        // Explicit function-invocation layer so we control the iteration cap.
        IChatClient guardedClient = new ChatClientBuilder(chatClient)
            .UseFunctionInvocation(configure: c =>
            {
                c.MaximumIterationsPerRequest = MaxToolIterations;
                Console.WriteLine($"{Tag} FunctionInvoking cap set to {c.MaximumIterationsPerRequest}");
            })
            .Build();

        AIAgent agent = new ChatClientAgent(
            guardedClient,
            instructions: instructions,
            name: "LocalChat",
            tools: tools);

        Console.WriteLine($"{Tag} Agent created.");
        return agent;
    }
}