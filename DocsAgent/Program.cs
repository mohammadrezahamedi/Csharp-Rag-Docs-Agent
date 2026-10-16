using System.Text;
using DocsAgent.Agents;
using DocsAgent.Evals;
using DocsAgent.Prompts;
using DocsAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using DocsAgent.Rag;

const string Tag = "[DocsAgent/Chat]";

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

string endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
string modelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME") ?? "llama3.2";

// Read the docs folder directly from the project folder (no copy step needed).
// Optional override: set the DOCS_FOLDER environment variable.
string docsFolder = Environment.GetEnvironmentVariable("DOCS_FOLDER")
    ?? @"F:\CSharpProjects\Docs Agent\DocsAgent\Docs";

Console.WriteLine($"{Tag} Docs folder = {docsFolder}, exists = {Directory.Exists(docsFolder)}");

var docsTools = new DocsTools(docsFolder);

IList<AITool> tools = new List<AITool>
{
    AIFunctionFactory.Create(docsTools.ListDocs),
    AIFunctionFactory.Create(docsTools.ReadDoc)
};

// Inject the real file names so the small model never has to guess them.
string fileList = docsTools.ListDocs().Replace("\n", ", ");
Console.WriteLine($"{Tag} Available docs at startup: {fileList}");

string instructions = AgentPrompts.BuildDocsAssistant(fileList);

AIAgent agent = LocalAgentFactory.Create(endpoint, modelName, instructions, tools);

// RAG test setup: chunk + embed all docs at startup.
IEmbeddingGenerator<string, Embedding<float>> embedder =
    new OllamaApiClient(new Uri(endpoint), "nomic-embed-text");
var indexer = new DocsIndexer(embedder, new TextChunker(500, 100), new InMemoryVectorStore());
await indexer.IndexFolderAsync(docsFolder);

// Conversation memory: the full history is sent on every turn.
var history = new List<ChatMessage>();

Console.WriteLine($"{Tag} Ready. Commands: /eval (run quality tests), /clear (reset memory), /exit (quit)");

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
    if (command == "/eval")
    {
        var runner = new EvalRunner(agent, docsTools);
        await runner.RunAsync(EvalSuite.Default());
        continue;
    }

    if (command.StartsWith("/search "))
    {
        var hits = await indexer.SearchAsync(input.Substring(8), 3);
        foreach (var hit in hits)
        {
            Console.WriteLine($"[RAG/Test] {hit.Score:F3} | {hit.Chunk.Source}#{hit.Chunk.Index} | {hit.Chunk.Text.Replace("\n", " ")}");
        }
        continue;
    }

    history.Add(new ChatMessage(ChatRole.User, input));

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