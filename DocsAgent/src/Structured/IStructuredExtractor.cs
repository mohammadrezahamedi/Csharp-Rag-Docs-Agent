namespace Structured;

public interface IStructuredExtractor<T>
{
    Task<ExtractionResult<T>> ExtractAsync(string input, CancellationToken ct = default);
}