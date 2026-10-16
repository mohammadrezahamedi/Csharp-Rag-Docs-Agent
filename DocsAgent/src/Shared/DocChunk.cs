namespace DocsAgent.Shared;

// One piece of a document, plus where it came from.
public sealed record DocChunk(string Source, int Index, string Text);

// A chunk with its similarity score (1.0 = same meaning, 0 = unrelated).
public sealed record SearchHit(DocChunk Chunk, float Score);