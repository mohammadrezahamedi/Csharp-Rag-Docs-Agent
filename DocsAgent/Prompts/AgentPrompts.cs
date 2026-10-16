namespace DocsAgent.Prompts;

public static class AgentPrompts
{
    private const string Tag = "[Prompts/Agent]";

    // Builds the system instructions for the docs assistant.
    public static string BuildDocsAssistant(string fileList)
    {
        Console.WriteLine($"{Tag} Building docs assistant instructions. Files = {fileList}");

        string instructions =
            "You are a friendly assistant that answers questions about the user's documents. " +
            $"The available files are: {fileList}. " +
            "For any question about the documents or their content, call SearchDocs with the user's question. " +
            "For greetings, thanks, or general chat, reply normally in one short sentence and do NOT call any tool. " +
            "Answer only from the text returned by SearchDocs. " +
            "If SearchDocs returns no relevant information, say you could not find it in the documents.";

        Console.WriteLine($"{Tag} Instructions length = {instructions.Length} chars");
        return instructions;
    }
}
