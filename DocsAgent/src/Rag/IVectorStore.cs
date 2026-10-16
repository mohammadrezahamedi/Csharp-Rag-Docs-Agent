using DocsAgent.Shared;

namespace DocsAgent.Rag;

public interface IVectorStore
{
    int Count { get; }
    void Add(DocChunk chunk, float[] vector);
    IReadOnlyList<SearchHit> Search(float[] queryVector, int topK);
}