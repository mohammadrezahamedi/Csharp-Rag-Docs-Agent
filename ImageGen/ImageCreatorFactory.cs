namespace ImageGen;

#pragma warning disable MEAI001 // ImageGeneratorFactory returns the experimental IImageGenerator

// Picks the image provider from IMAGE_PROVIDER: "azure" (default) or "pollinations".
public static class ImageCreatorFactory
{
    private const string Tag = "[ImageGen/Selector]";

    public static IImageCreator Create()
    {
        LocalEnv.Load();
        string provider = GetProvider();
        Console.WriteLine($"{Tag} Provider = {provider}");

        return provider switch
        {
            "azure" => new ImageCreator(ImageGeneratorFactory.Create()),
            "pollinations" => CreatePollinations(),
            _ => throw new InvalidOperationException($"Unknown IMAGE_PROVIDER '{provider}'. Use 'azure' or 'pollinations'.")
        };
    }

    // Seeded creation (used by /imagine and /tweak) only exists for the HTTP provider.
    public static ISeededImageCreator CreateSeeded()
    {
        LocalEnv.Load();
        string provider = GetProvider();
        Console.WriteLine($"{Tag} Seeded provider = {provider}");

        if (provider != "pollinations")
        {
            throw new InvalidOperationException($"{Tag} /imagine and /tweak need IMAGE_PROVIDER=pollinations (current: {provider}).");
        }

        return CreatePollinations();
    }

    private static string GetProvider()
    {
        return (Environment.GetEnvironmentVariable("IMAGE_PROVIDER") ?? "azure").Trim().ToLowerInvariant();
    }

    private static HttpImageCreator CreatePollinations()
    {
        string baseUrl = Environment.GetEnvironmentVariable("POLLINATIONS_URL") ?? "https://gen.pollinations.ai";
        string model = Environment.GetEnvironmentVariable("POLLINATIONS_MODEL") ?? "flux";
        string? key = Environment.GetEnvironmentVariable("POLLINATIONS_API_KEY");
        Console.WriteLine($"{Tag} Pollinations. Url={baseUrl}, Model={model}");

        return new HttpImageCreator(new HttpClient { Timeout = TimeSpan.FromSeconds(120) }, baseUrl, model, key);
    }
}
