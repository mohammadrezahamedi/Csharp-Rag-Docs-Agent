using System.ComponentModel;
using System.Text;
using DocsAgent.Rag;
using DocsAgent.Shared;
namespace DocsAgent.Tools;
public class RagTools : IToolCallSource
{
    private const string Tag = "[Tools/Rag]";
    private const int TopK = 3;

    private readonly IDocsSearcher _searcher;
    private readonly float _minScore;

    // Raised every time the model calls the tool (used by the eval runner).
    public event Action<string>? ToolCalled;

    public RagTools(IDocsSearcher searcher, float minScore = 0.5f)
    {
        _searcher = searcher;
        _minScore = minScore;
        Console.WriteLine($"{Tag} Created. minScore={_minScore}, topK={TopK}");
    }

    [Description("Searches all documents and returns the most relevant passages for a question. Use this for any question about the documents.")]
    public async Task<string> SearchDocs([Description("The user's question in plain English.")] string question)
    {
        Console.WriteLine($"{Tag} SearchDocs called with question='{question}'");
        ToolCalled?.Invoke("SearchDocs");

        IReadOnlyList<SearchHit> hits = await _searcher.SearchAsync(question, TopK);
        var relevant = hits.Where(h => h.Score >= _minScore).ToList();

        foreach (SearchHit hit in hits)
        {
            Console.WriteLine($"{Tag} {hit.Score:F3} {hit.Chunk.Source}#{hit.Chunk.Index} {(hit.Score >= _minScore ? "KEEP" : "DROP")}");
        }

        if (relevant.Count == 0)
        {
            Console.WriteLine($"{Tag} Nothing above the threshold");
            return "No relevant information was found in the documents.";
        }

        var sb = new StringBuilder();
        foreach (SearchHit hit in relevant)
        {
            sb.AppendLine($"[Source: {hit.Chunk.Source}]");
            sb.AppendLine(hit.Chunk.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
