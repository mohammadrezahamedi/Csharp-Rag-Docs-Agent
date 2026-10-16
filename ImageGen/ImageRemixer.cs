using System.Text.RegularExpressions;
using DocsAgent.Vision;

namespace ImageGen;

public sealed record RemixResult(string Description, string ImagePath);

// Takes a source image and produces a new image in the same spirit (describe, then generate).
public interface IImageRemixer
{
    Task<RemixResult> RemixAsync(string sourceImagePath, string style, string outputPath, CancellationToken ct = default);
}

public sealed class ImageRemixer : IImageRemixer
{
    private const string Tag = "[ImageGen/Remix]";
    private const int MaxPromptChars = 600; // keeps the GET url short enough for the HTTP provider

    private readonly IImageDescriber _describer;
    private readonly IImageCreator _creator;

    public ImageRemixer(IImageDescriber describer, IImageCreator creator)
    {
        _describer = describer;
        _creator = creator;
    }

    public async Task<RemixResult> RemixAsync(string sourceImagePath, string style, string outputPath, CancellationToken ct = default)
    {
        Console.WriteLine($"{Tag} Source={sourceImagePath}, Style={(string.IsNullOrWhiteSpace(style) ? "none" : style)}");

        const string question =
            "Describe this image as a short text-to-image prompt, at most 60 words. " +
            "Mention the main objects, colors, layout and mood. Output only the prompt text.";
        string description = await _describer.DescribeAsync(sourceImagePath, question, ct);

        // Models like to add markdown and line breaks; flatten them and cap the length.
        string cleaned = Regex.Replace(description.Replace("*", "").Replace("#", ""), @"\s+", " ").Trim();
        if (cleaned.Length > MaxPromptChars)
        {
            cleaned = cleaned.Substring(0, MaxPromptChars);
        }

        string prompt = string.IsNullOrWhiteSpace(style) ? cleaned : $"{cleaned}. Style: {style}";
        Console.WriteLine($"{Tag} Prompt length = {prompt.Length}");

        string saved = await _creator.CreateAsync(prompt, outputPath, ct);
        return new RemixResult(cleaned, saved);
    }
}
