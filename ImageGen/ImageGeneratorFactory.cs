using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

namespace ImageGen;

#pragma warning disable MEAI001

// Image generation runs on Azure OpenAI only: local Ollama image models are experimental and need more VRAM than this PC has.
public static class ImageGeneratorFactory
{
    private const string Tag = "[ImageGen/Factory]";

    public static IImageGenerator Create()
    {
        string endpoint = Require("AZURE_OPENAI_ENDPOINT");
        string key = Require("AZURE_OPENAI_KEY");
        string deployment = Require("AZURE_OPENAI_IMAGE_DEPLOYMENT");
        Console.WriteLine($"{Tag} Azure. Endpoint={endpoint}, ImageDeployment={deployment}, Key=***");

        // VERIFY: the conversion method name comes from the docs quickstart summary, not a signature I saw.
        return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key))
            .GetImageClient(deployment)
            .AsIImageGenerator();
    }

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
