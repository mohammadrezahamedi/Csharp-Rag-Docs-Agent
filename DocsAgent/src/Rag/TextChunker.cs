// IVectorStore.cs, InMemoryVectorStore.cs
using DocsAgent.Shared;

namespace DocsAgent.Rag;
// Splits text into overlapping pieces of roughly chunkSize characters.
public sealed class TextChunker : ITextChunker
{
    private const string Tag = "[Rag/Chunker]";

    private readonly int _chunkSize;
    private readonly int _overlap;

    public TextChunker(int chunkSize = 500, int overlap = 100)
    {
        if (overlap >= chunkSize)
        {
            throw new ArgumentException("overlap must be smaller than chunkSize");
        }

        _chunkSize = chunkSize;
        _overlap = overlap;
        Console.WriteLine($"{Tag} Created. chunkSize={_chunkSize}, overlap={_overlap}");
    }

    public IReadOnlyList<string> Split(string text)
    {
        var chunks = new List<string>();
        string clean = text.Replace("\r\n", "\n").Trim();

        if (clean.Length == 0)
        {
            Console.WriteLine($"{Tag} Empty text, 0 chunks");
            return chunks;
        }

        int start = 0;
        while (start < clean.Length)
        {
            int end = Math.Min(start + _chunkSize, clean.Length);

            // Try to cut at a line break or sentence end so we do not split mid-sentence.
            if (end < clean.Length)
            {
                int window = clean.LastIndexOfAny(new[] { '\n', '.' }, end - 1, end - start);
                if (window > start + _chunkSize / 2)
                {
                    end = window + 1;
                }
            }

            string piece = clean.Substring(start, end - start).Trim();
            if (piece.Length > 0)
            {
                chunks.Add(piece);
            }

            if (end >= clean.Length)
            {
                break;
            }

            // Step back by the overlap, but always move forward.
            start = Math.Max(end - _overlap, start + 1);
        }

        Console.WriteLine($"{Tag} {clean.Length} chars -> {chunks.Count} chunk(s)");
        return chunks;
    }
}
