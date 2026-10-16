using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace DocsAgent.Agents;

// Picks the chat model provider from the LLM_PROVIDER env var ("ollama" by default, or "azure").
public static class ChatClientFactory
{
    private const string Tag = "[Agents/ClientFactory]";

    public static IChatClient Create()
    {
        string provider = (Environment.GetEnvironmentVariable("LLM_PROVIDER") ?? "ollama").Trim().ToLowerInvariant();
        Console.WriteLine($"{Tag} Provider = {provider}");

        return provider switch
        {
            "azure" => CreateAzure(),
            "ollama" => CreateOllama(),
            _ => throw new InvalidOperationException($"Unknown LLM_PROVIDER '{provider}'. Use 'ollama' or 'azure'.")
        };
    }
    // Vision client: same provider switch, but Ollama uses its own vision model name.
    public static IChatClient CreateVision()
    {
        string provider = (Environment.GetEnvironmentVariable("LLM_PROVIDER") ?? "ollama").Trim().ToLowerInvariant();
        Console.WriteLine($"{Tag} Vision provider = {provider}");

        return provider switch
        {
            "azure" => CreateAzure(), // deployment must be a vision-capable model
            "ollama" => CreateOllamaVision(),
            _ => throw new InvalidOperationException($"Unknown LLM_PROVIDER '{provider}'. Use 'ollama' or 'azure'.")
        };
    }

    private static IChatClient CreateOllamaVision()
    {
        string endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
        string model = Environment.GetEnvironmentVariable("OLLAMA_VISION_MODEL_NAME") ?? "qwen3-vl:8b";
        Console.WriteLine($"{Tag} Ollama vision. Endpoint={endpoint}, Model={model}");

        return new OllamaApiClient(new Uri(endpoint), model);
    }
    private static IChatClient CreateOllama()
    {
        string endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
        string model = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME") ?? "llama3.2";
        Console.WriteLine($"{Tag} Ollama. Endpoint={endpoint}, Model={model}");

        return new OllamaApiClient(new Uri(endpoint), model);
    }

    private static IChatClient CreateAzure()
    {
        string endpoint = Require("AZURE_OPENAI_ENDPOINT");
        string key = Require("AZURE_OPENAI_KEY");
        string deployment = Require("AZURE_OPENAI_DEPLOYMENT");
        Console.WriteLine($"{Tag} Azure. Endpoint={endpoint}, Deployment={deployment}, Key=***");

        return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key))
            .GetChatClient(deployment)
            .AsIChatClient();
    }

    // Fails early with a clear message instead of a confusing error later.
    private static string Require(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing environment variable {name}.");
        }

        return value;
    }
}