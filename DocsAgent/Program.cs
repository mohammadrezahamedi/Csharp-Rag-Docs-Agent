using System.Text;
using DocsAgent.Agents;
using DocsAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

const string Tag = "[DocsAgent/Chat]";

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

string endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
string modelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME") ?? "llama3.2";

string docsFolder = Environment.GetEnvironmentVariable("DOCS_FOLDER")
    ?? @"F:\CSharpProjects\Docs Agent\DocsAgent\Docs";

var docsTools = new DocsTools(docsFolder);

IList<AITool> tools = new List<AITool>
{
    AIFunctionFactory.Create(docsTools.ListDocs),
    AIFunctionFactory.Create(docsTools.ReadDoc)
};

// Inject the real file names so the small model never has to guess them.
string fileList = docsTools.ListDocs().Replace("\n", ", ");
Console.WriteLine($"{Tag} Available docs at startup: {fileList}");

string Instructions =
    "You answer questions using the user's documents. " +
    $"The ONLY available files are: {fileList}. " +
    "Never invent file names. Call ReadDoc with one of these exact names. " +
    "Answer only from the document text. If the answer is not in the documents, say you could not find it.";
AIAgent agent = LocalAgentFactory.Create(endpoint, modelName, Instructions, tools);

// Conversation memory: the full history is sent on every turn.
var history = new List<ChatMessage>();

Console.WriteLine($"{Tag} Ready. Commands: /clear (reset memory), /exit (quit)");

while (true)
{
    Console.Write("\nYou: ");
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    string command = input.Trim().ToLowerInvariant();

    if (command == "/exit")
    {
        Console.WriteLine($"{Tag} Bye.");
        break;
    }

    if (command == "/clear")
    {
        history.Clear();
        Console.WriteLine($"{Tag} Memory cleared.");
        continue;
    }

    history.Add(new ChatMessage(ChatRole.User, input));
    Console.WriteLine($"{Tag} Sending. History messages = {history.Count}");
    Console.Write("AI: ");

    var reply = new StringBuilder();

    try
    {
        await foreach (var update in agent.RunStreamingAsync(history))
        {
            string piece = update.ToString();
            Console.Write(piece);
            reply.Append(piece);
        }

        Console.WriteLine();
        history.Add(new ChatMessage(ChatRole.Assistant, reply.ToString()));
        Console.WriteLine($"{Tag} Reply received. Chars = {reply.Length}");
    }
    catch (Exception ex)
    {
        // Remove the failed user message so history stays consistent.
        history.RemoveAt(history.Count - 1);
        Console.WriteLine();
        Console.WriteLine($"{Tag} ERROR: {ex.Message}");
        Console.WriteLine($"{Tag} Is Ollama running and is the model pulled?");
    }
}