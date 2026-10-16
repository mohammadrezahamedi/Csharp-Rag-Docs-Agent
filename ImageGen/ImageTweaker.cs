namespace ImageGen;

// Makes a base image, then re-renders it with extra change phrases while keeping the same seed.
public interface IImageTweaker
{
    Task<string> StartAsync(string prompt, string outputPath, CancellationToken ct = default);
    Task<string> TweakAsync(string change, string outputPath, CancellationToken ct = default);
}

// State (base prompt, seed, changes) lives in memory only; nothing but the images is written to disk.
public sealed class ImageTweaker : IImageTweaker
{
    private const string Tag = "[ImageGen/Tweaker]";
    private readonly ISeededImageCreator _creator;
    private readonly List<string> _changes = new();
    private string? _basePrompt;
    private int _seed;

    public ImageTweaker(ISeededImageCreator creator)
    {
        _creator = creator;
    }

    public async Task<string> StartAsync(string prompt, string outputPath, CancellationToken ct = default)
    {
        int seed = Random.Shared.Next(1, 1_000_000);
        Console.WriteLine($"{Tag} New base image. Seed={seed}, Prompt={prompt}");

        string saved = await _creator.CreateAsync(prompt, outputPath, seed, ct);

        // State changes only after the image was really created.
        _basePrompt = prompt;
        _seed = seed;
        _changes.Clear();
        return saved;
    }

    public async Task<string> TweakAsync(string change, string outputPath, CancellationToken ct = default)
    {
        if (_basePrompt is null)
        {
            throw new InvalidOperationException($"{Tag} No base image yet. Run /imagine <prompt> first.");
        }

        string fullPrompt = _basePrompt + ", " + string.Join(", ", _changes.Append(change));
        Console.WriteLine($"{Tag} Tweak #{_changes.Count + 1}. Seed={_seed}, Prompt={fullPrompt}");

        string saved = await _creator.CreateAsync(fullPrompt, outputPath, _seed, ct);
        _changes.Add(change);
        return saved;
    }
}
