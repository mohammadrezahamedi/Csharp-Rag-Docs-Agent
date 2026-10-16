namespace ImageGen;

// Turns a text prompt into a PNG file and returns the saved path.
public interface IImageCreator
{
    Task<string> CreateAsync(string prompt, string outputPath, CancellationToken ct = default);
}
