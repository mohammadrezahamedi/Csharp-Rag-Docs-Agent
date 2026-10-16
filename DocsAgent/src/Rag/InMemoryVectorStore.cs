using DocsAgent.Shared;

namespace DocsAgent.Rag;

// Simple in-memory store. Fine for small projects; swap it later by implementing IVectorStore.
public sealed class InMemoryVectorStore : IVectorStore
{
    private const string Tag = "[Rag/Store]";

    private readonly List<(DocChunk Chunk, float[] Vector)> _items = new();

    public int Count => _items.Count;

    public void Add(DocChunk chunk, float[] vector)
    {
        _items.Add((chunk, vector));
        Console.WriteLine($"{Tag} Added {chunk.Source}#{chunk.Index}, vector size={vector.Length}, total={_items.Count}");
    }

    public IReadOnlyList<SearchHit> Search(float[] queryVector, int topK)
    {
        var hits = _items
            .Select(i => new SearchHit(i.Chunk, Cosine(queryVector, i.Vector)))
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();

        Console.WriteLine($"{Tag} Search done. Returned {hits.Count} of {_items.Count}");
        return hits;
    }

    // Cosine similarity: how much two vectors point in the same direction.
    private static float Cosine(float[] a, float[] b)
    {
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        float denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denom == 0 ? 0 : dot / denom;
    }
}
