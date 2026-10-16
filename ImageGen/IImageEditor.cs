namespace ImageGen;

// Edits a LOCAL image with a text instruction and saves the result as a new file.
public interface IImageEditor
{
    Task<string> EditAsync(string sourceImagePath, string instruction, string outputPath, CancellationToken ct = default);
}
