namespace ImageGen;

// Builds the local-image editor from env vars (needs IMAGE_PROVIDER=pollinations and POLLINATIONS_API_KEY).
public static class ImageEditorFactory
{
    private const string Tag = "[ImageGen/EditorFactory]";

    public static IImageEditor Create()
    {
        LocalEnv.Load();

        string provider = (Environment.GetEnvironmentVariable("IMAGE_PROVIDER") ?? "azure").Trim().ToLowerInvariant();
        if (provider != "pollinations")
        {
            throw new InvalidOperationException($"{Tag} /edit needs IMAGE_PROVIDER=pollinations (current: {provider}).");
        }

        string? key = Environment.GetEnvironmentVariable("POLLINATIONS_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Missing environment variable POLLINATIONS_API_KEY.");
        }

        string genUrl = Environment.GetEnvironmentVariable("POLLINATIONS_URL") ?? "https://gen.pollinations.ai";
        string mediaUrl = Environment.GetEnvironmentVariable("POLLINATIONS_MEDIA_URL") ?? "https://media.pollinations.ai";
        string model = Environment.GetEnvironmentVariable("POLLINATIONS_EDIT_MODEL") ?? "kontext";
        Console.WriteLine($"{Tag} Pollinations editor. Model={model}");

        return new PollinationsImageEditor(new HttpClient { Timeout = TimeSpan.FromSeconds(180) }, genUrl, mediaUrl, model, key);
    }
}
