namespace DocsAgent.Rag;

public interface ITextChunker
{
    IReadOnlyList<string> Split(string text);
}
