namespace DocsAgent.Vision;

// Sends one image plus a question to a vision model and returns the answer.
public interface IImageDescriber
{
    Task<string> DescribeAsync(string imagePath, string question, CancellationToken ct = default);
}