namespace DocsAgent.Prompts;

public static class AgentPrompts
{
    private const string Tag = "[Prompts/Agent]";

    // Builds the system instructions for the docs assistant.
    // fileList is injected so the small model never has to guess file names.
    public static string BuildDocsAssistant(string fileList)
    {
        Console.WriteLine($"{Tag} Building docs assistant instructions. Files = {fileList}");

        string instructions =
            "You are a friendly assistant that can read the user's documents. " +
            $"The ONLY available files are: {fileList}. " +
            "Only call ReadDoc when the user asks a question about the documents or their content. " +
            "For greetings, thanks, or general chat, reply normally in one short sentence and do NOT call any tool. " +
            "Never invent file names. When you do use a document, answer only from its text. " +
            "If the answer is not in the documents, say you could not find it.";

        Console.WriteLine($"{Tag} Instructions length = {instructions.Length} chars");
        return instructions;
    }
}