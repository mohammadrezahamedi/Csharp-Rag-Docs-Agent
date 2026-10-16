using Microsoft.Extensions.AI;

namespace DocsAgent.Rag;

// Reads .txt files, chunks them, embeds each chunk, and stores the vectors.
public sealed class DocsIndexer
{
    private const string Tag = "[Rag/Indexer]";

    private readonly IEmbeddingGenerator<string, Embedding<float>> _embedder;
    private readonly ITextChunker _chunker;
    private readonly IVectorStore _store;

    public DocsIndexer(
        IEmbeddingGenerator<string, Embedding<float>> embedder,
        ITextChunker chunker,
        IVectorStore store)
    {
        _embedder = embedder;
        _chunker = chunker;
        _store = store;
    }

    public async Task IndexFolderAsync(string folder)
    {
        Console.WriteLine($"{Tag} Indexing folder: {folder}");

        if (!Directory.Exists(folder))
        {
            Console.WriteLine($"{Tag} ERROR: folder not found");
            return;
        }

        foreach (string path in Directory.GetFiles(folder, "*.txt"))
        {
            string name = Path.GetFileName(path);
            IReadOnlyList<string> pieces = _chunker.Split(File.ReadAllText(path));
            Console.WriteLine($"{Tag} {name}: {pieces.Count} chunk(s), embedding...");

            if (pieces.Count == 0)
            {
                continue;
            }

            // One call embeds all chunks of the file together (faster than one by one).
            var embeddings = await _embedder.GenerateAsync(pieces);

            for (int i = 0; i < pieces.Count; i++)
            {
                _store.Add(new DocChunk(name, i, pieces[i]), embeddings[i].Vector.ToArray());
            }
        }

        Console.WriteLine($"{Tag} Done. Total chunks in store = {_store.Count}");
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string question, int topK)
    {
        Console.WriteLine($"{Tag} Search question: '{question}'");
        var embeddings = await _embedder.GenerateAsync(new[] { question });
        return _store.Search(embeddings[0].Vector.ToArray(), topK);
    }
}
