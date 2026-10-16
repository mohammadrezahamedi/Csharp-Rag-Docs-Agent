namespace ImageGen;

// Same as IImageCreator, but the caller controls the seed so repeated calls keep a similar composition.
public interface ISeededImageCreator : IImageCreator
{
    Task<string> CreateAsync(string prompt, string outputPath, int seed, CancellationToken ct = default);
}
