using DocsAgent.Shared;

namespace DocsAgent.Rag;

// Lets tools search the documents without knowing how indexing works.
public interface IDocsSearcher
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(string question, int topK);
}