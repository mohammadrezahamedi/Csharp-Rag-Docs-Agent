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
using Planner;
using Structured;
using DocsAgent.Vision;
using ImageGen;

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
var ticketTools = new Tools.TicketTools(new Tools.InMemoryTicketStore());
tools.Add(AIFunctionFactory.Create(ticketTools.GetTicketStatus));
tools.Add(AIFunctionFactory.Create(ticketTools.CalculateRefund));
// Inject the real file names so the small model never has to guess them.
string fileList = docsTools.ListDocs().Replace("\n", ", ");
Console.WriteLine($"{Tag} Available docs at startup: {fileList}");
// Planner: generator uses the raw chat client, steps run through the existing agent.
var toolNames = new List<string> { "ListDocs", "SearchDocs", "GetTicketStatus", "CalculateRefund" };



string instructions = AgentPrompts.BuildDocsAssistant(fileList);

IChatClient chatClient = ChatClientFactory.Create();
AIAgent agent = LocalAgentFactory.Create(chatClient, instructions, tools);

IChatMemory memory = new SummaryMemory(chatClient, 6, 2);

Console.WriteLine($"{Tag} Ready. Commands: /eval (run quality tests), /search <text> (test retrieval), /memory on|off, /clear (reset memory), /exit (quit)");
IPlanGenerator planGenerator = new PlanGenerator(chatClient, toolNames, 5, 3);
IImageDescriber imageDescriber = new ImageDescriber(ChatClientFactory.CreateVision());
IAgentRunner agentRunner = new AgentRunnerAdapter(agent);

// Final-answer agent has NO tools, so "Do not call tools" is actually enforced.
AIAgent finalAgent = LocalAgentFactory.Create(
    chatClient,
    "You write a final answer using only the step results you are given. Never ask for more data.",
    new List<AITool>());
IAgentRunner finalRunner = new AgentRunnerAdapter(finalAgent);

IPlanner planner = new PlannerService(planGenerator, agentRunner, finalRunner);




while (true)
{
    Console.Write("\nYou: ");
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    string command = input.Trim().ToLowerInvariant();
    if (command.StartsWith("/generate "))
    {
        // Provider is chosen by IMAGE_PROVIDER; created lazily so missing variables do not break startup.
        try
        {
            IImageCreator creator = ImageCreatorFactory.Create();
            string outPath = Path.Combine(AppContext.BaseDirectory, "generated", $"img_{DateTime.Now:HHmmss}.png");
            string saved = await creator.CreateAsync(input.Substring(10).Trim(), outPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Image saved: {saved}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{Tag} IMAGE ERROR: {ex.Message}");
            Console.ResetColor();
        }

        continue;
    }
    if (command == "/exit")
    {
        Console.WriteLine($"{Tag} Bye.");
        break;
    }
    if (command.StartsWith("/vision "))
    {
        // Format: /vision <image path> | <question>  (the | separator allows spaces in the path)
        string[] parts = input.Substring(8).Split('|', 2);
        string imagePath = parts[0].Trim().Trim('"');
        string question = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
            ? parts[1].Trim()
            : "Describe this image.";

        try
        {
            await VisionDemo.RunAsync(imageDescriber, imagePath, question);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{Tag} VISION ERROR: {ex.Message}");
            Console.ResetColor();
        }

        continue;
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
    if (command == "/memory summary")
    {
        memory = new SummaryMemory(chatClient, 6, 2);
        Console.WriteLine($"{Tag} Memory is SUMMARY (old messages get summarized). History starts empty.");
        continue;
    }
    if (command == "/plan-eval")
    {
        Console.WriteLine($"{Tag} Running planner eval");
        await PlannerEvalRunner.RunAsync(planner);
        continue;
    }
    if (command.StartsWith("/plan "))
    {
        var planResult = await planner.RunAsync(input.Substring(6));
        Console.WriteLine($"{Tag} Success={planResult.Success}");

        // Green = final model answer, red = error.
        Console.ForegroundColor = planResult.Success ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(planResult.Success ? planResult.FinalAnswer : $"Error: {planResult.Error}");
        Console.ResetColor();
        continue;
    }
    if (command == "/extract-eval")
    {
        Console.WriteLine($"{Tag} Running structured eval");
        IStructuredExtractor<TicketInfo> extractor = new TicketExtractor(chatClient);
        await new TicketEvalRunner(extractor).RunAsync(TicketEvalCases.All);
        continue;
    }
    if (command == "/tools-eval")
    {
        Console.WriteLine($"{Tag} Running tools eval");
        await ToolsEvalRunner.RunAsync(new AgentRunnerAdapter(agent));
        continue;
    }
    if (command == "/retry-test")
    {
        Console.WriteLine($"{Tag} Running retry-path test");
        await RetryPathTest.RunAsync();
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

            // Color only the answer text; logs printed by the tools in between stay default.
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(piece);
            Console.ResetColor();

            reply.Append(piece);
        }

        Console.WriteLine();
        memory.Add(new ChatMessage(ChatRole.Assistant, reply.ToString()));
        await memory.CompactAsync();
        Console.WriteLine($"{Tag} Reply received. Chars = {reply.Length}");
    }
    catch (Exception ex)
    {
        // Remove the failed user message so history stays consistent.
        memory.RemoveLast();
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{Tag} ERROR: {ex.Message}");
        Console.WriteLine($"{Tag} Is Ollama running and is the model pulled?");
        Console.ResetColor();
    }
    static async Task RunExtractDemoAsync(Microsoft.Extensions.AI.IChatClient client)
    {
        IStructuredExtractor<TicketInfo> extractor = new Structured.TicketExtractor(client);

        var text = "Hi, I was charged twice for my Pro plan this month and the app also crashes on login. " +
                   "Please fix this today and refund the extra charge!";

        var result = await extractor.ExtractAsync(text);

        Console.WriteLine($"[Demo] Success={result.Success} Attempts={result.Attempts}");
        if (result.Success)
            Console.WriteLine($"[Demo] {result.Value}");
        else
            Console.WriteLine($"[Demo] Error: {result.LastError}");
    }

}