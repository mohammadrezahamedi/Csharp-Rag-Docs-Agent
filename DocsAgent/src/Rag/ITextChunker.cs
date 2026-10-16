// IVectorStore.cs, InMemoryVectorStore.cs
using DocsAgent.Shared;

namespace DocsAgent.Rag;
public interface ITextChunker
{
    IReadOnlyList<string> Split(string text);
}
