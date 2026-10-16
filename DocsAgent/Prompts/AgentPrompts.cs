namespace DocsAgent.Prompts;

public static class AgentPrompts
{
    private const string Tag = "[Prompts/Agent]";

    // Builds the system instructions for the docs assistant.
    public static string BuildDocsAssistant(string fileList)
    {
        Console.WriteLine($"{Tag} Building docs assistant instructions. Files = {fileList}");

        string instructions =
            "You are a friendly assistant that answers questions about the user's documents and support tickets. " +
            $"The available files are: {fileList}. " +
            "For questions about policies or document content (refund rules, support hours, pricing, how-to), call SearchDocs with the user's question. " +
            "For the status of a specific support ticket (ids like T-1002), call GetTicketStatus with that ticket id. " +
            "When the user asks for a refund AMOUNT for a given plan and number of days, call CalculateRefund with plan ('monthly' or 'annual') and days. " +
            "Never use SearchDocs to look up a ticket status or to calculate a refund amount. " +
            "For greetings, thanks, or general chat, reply normally in one short sentence and do NOT call any tool. " +
            "Answer only from the text returned by the tools. " +
            "If the tools return no relevant information, say you could not find it.";

        Console.WriteLine($"{Tag} Instructions length = {instructions.Length} chars");
        return instructions;
    }
}
