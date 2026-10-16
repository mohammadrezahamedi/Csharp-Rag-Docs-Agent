using Microsoft.Extensions.AI;

namespace ImageGen;

#pragma warning disable MEAI001 // IImageGenerator is experimental in Microsoft.Extensions.AI

public sealed class ImageCreator : IImageCreator
{
    private const string Tag = "[ImageGen/Creator]";
    private readonly IImageGenerator _generator;

    public ImageCreator(IImageGenerator generator)
    {
        _generator = generator;
    }

    public async Task<string> CreateAsync(string prompt, string outputPath, CancellationToken ct = default)
    {
        Console.WriteLine($"{Tag} Prompt={prompt}");
        Console.WriteLine($"{Tag} Output={outputPath}");

        ImageGenerationResponse response = await _generator.GenerateImagesAsync(prompt, cancellationToken: ct);

        DataContent? image = response.Contents.OfType<DataContent>().FirstOrDefault();
        if (image is null)
        {
            // The provider may return a UriContent instead of raw bytes.
            throw new InvalidOperationException($"{Tag} No image bytes in the response (got {response.Contents.Count} content item(s)).");
        }

        string? folder = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        await File.WriteAllBytesAsync(outputPath, image.Data.ToArray(), ct);
        Console.WriteLine($"{Tag} Saved. Bytes={image.Data.Length}, Mime={image.MediaType}");
        return outputPath;
    }
}
