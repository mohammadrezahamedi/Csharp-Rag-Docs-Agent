using System.Text;
using DocsAgent.Agents;
using DocsAgent.Evals;
using DocsAgent.Memory;
using DocsAgent.Prompts;
using DocsAgent.Rag;
using DocsAgent.Shared;
using DocsAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

const string Tag = "[DocsAgent/Chat]";

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Still used by the embedder; the chat model is chosen inside ChatClientFactory.
string endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";

string docsFolder = DocsFolderLocator.Find();

Console.WriteLine($"{Tag} Docs folder = {docsFolder}, exists = {Directory.Exists(docsFolder)}");

var docsTools = new DocsTools(docsFolder);

// RAG setup: chunk + embed all docs at startup.
IEmbeddingGenerator<string, Embedding<float>> embedder =
    new OllamaApiClient(new Uri(endpoint), "nomic-embed-text");
var indexer = new DocsIndexer(embedder, new TextChunker(500, 100), new InMemoryVectorStore());
await indexer.IndexFolderAsync(docsFolder);

var ragTools = new RagTools(indexer, 0.55f);

// The agent searches by meaning (SearchDocs) instead of reading whole files (ReadDoc).
IList<AITool> tools = new List<AITool>
{
    AIFunctionFactory.Create(docsTools.ListDocs),
    AIFunctionFactory.Create(ragTools.SearchDocs)
};

// Inject the real file names so the small model never has to guess them.
string fileList = docsTools.ListDocs().Replace("\n", ", ");
Console.WriteLine($"{Tag} Available docs at startup: {fileList}");

string instructions = AgentPrompts.BuildDocsAssistant(fileList);

IChatClient chatClient = ChatClientFactory.Create();
AIAgent agent = LocalAgentFactory.Create(chatClient, instructions, tools);

// Conversation memory: starts ON with a sliding window of the last 20 messages.
IChatMemory memory = new SlidingWindowMemory(20);

Console.WriteLine($"{Tag} Ready. Commands: /eval (run quality tests), /search <text> (test retrieval), /memory on|off, /clear (reset memory), /exit (quit)");

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
        memory.Clear();
        Console.WriteLine($"{Tag} Memory cleared.");
        continue;
    }

    if (command == "/memory off")
    {
        memory = new NoMemory();
        Console.WriteLine($"{Tag} Memory is OFF. Each question is answered alone.");
        continue;
    }

    if (command == "/memory on")
    {
        memory = new SlidingWindowMemory(20);
        Console.WriteLine($"{Tag} Memory is ON (last 20 messages). History starts empty.");
        continue;
    }

    if (command == "/eval")
    {
        var runner = new EvalRunner(new AgentRunnerAdapter(agent), new IToolCallSource[] { docsTools, ragTools });
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

    memory.Add(new ChatMessage(ChatRole.User, input));
    Console.WriteLine($"{Tag} Sending. History messages = {memory.Count}");
    Console.Write("AI: ");

    var reply = new StringBuilder();

    try
    {
        await foreach (var update in agent.RunStreamingAsync(memory.GetHistory()))
        {
            string piece = update.ToString();
            Console.Write(piece);
            reply.Append(piece);
        }

        Console.WriteLine();
        memory.Add(new ChatMessage(ChatRole.Assistant, reply.ToString()));
        Console.WriteLine($"{Tag} Reply received. Chars = {reply.Length}");
    }
    catch (Exception ex)
    {
        // Remove the failed user message so history stays consistent.
        memory.RemoveLast();
        Console.WriteLine();
        Console.WriteLine($"{Tag} ERROR: {ex.Message}");
        Console.WriteLine($"{Tag} Is Ollama running and is the model pulled?");
    }
}