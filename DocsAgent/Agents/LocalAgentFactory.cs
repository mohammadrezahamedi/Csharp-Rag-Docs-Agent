using DocsAgent.Shared;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DocsAgent.Agents;

public static class LocalAgentFactory
{
    private const string Tag = "[DocsAgent/Factory]";

    // Hard cap on model <-> tool round trips per question (loop guard).
    private const int MaxToolIterations = 3;

    // Builds an agent on top of any IChatClient (Ollama, Azure, ...) and gives it the tools.
    // Tracing is ON by default (set env TRACE=0 to turn it off), or pass your own ITracer.
    public static AIAgent Create(
        IChatClient chatClient,
        string instructions,
        IList<AITool>? tools = null,
        ITracer? tracer = null)
    {
        tracer ??= Environment.GetEnvironmentVariable("TRACE") == "0" ? new NullTracer() : new JsonlTracer();
        Console.WriteLine($"{Tag} Creating agent. Tools={tools?.Count ?? 0}, MaxToolIterations={MaxToolIterations}, Tracer={tracer.GetType().Name}");

        // Tracing client sits under the function-invocation layer, so every model round trip is recorded.
        IChatClient guardedClient = new ChatClientBuilder(new TracingChatClient(chatClient, tracer))
            .UseFunctionInvocation(configure: c =>
            {
                c.MaximumIterationsPerRequest = MaxToolIterations;
                Console.WriteLine($"{Tag} FunctionInvoking cap set to {c.MaximumIterationsPerRequest}");

                // Every tool call goes through here, so we can record name, arguments, result and time.
                c.FunctionInvoker = async (context, ct) =>
                {
                    string args = context.Arguments == null
                        ? ""
                        : string.Join(", ", context.Arguments.Select(kv => $"{kv.Key}={kv.Value}"));

                    return await tracer.MeasureAsync<object?>(
                        "tool", context.Function.Name, args,
                        async () => await context.Function.InvokeAsync(context.Arguments, ct),
                        r => r?.ToString());
                };
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
