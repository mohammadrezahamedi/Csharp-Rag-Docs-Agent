using DocsAgent.Agents;
using DocsAgent.Prompts;
using DocsAgent.Rag;
using DocsAgent.Shared;
using DocsAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using OllamaSharp;
using Planner;
using System.Diagnostics;
using System.Threading.RateLimiting;

const string Tag = "[Api/Main]";

var builder = WebApplication.CreateBuilder(args);

// Rate limit: max 5 requests per 30 seconds per client IP (built-in ASP.NET, no packages).
// Services must be registered BEFORE builder.Build().
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (ctx, _) =>
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[Api/RateLimit] REJECTED client = {ctx.HttpContext.Connection.RemoteIpAddress}");
        Console.ResetColor();
        return ValueTask.CompletedTask;
    };
    options.AddPolicy("ask", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(30),
                QueueLimit = 0
            }));
});
builder.Services.AddMemoryCache();
var app = builder.Build();
app.UseRateLimiter();

// ---- Agent setup (same wiring as the console app, built once at startup) ----
string endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
// Explicit path wins; the locator is only a fallback (it cannot find the sibling DocsAgent\Docs folder).
string docsFolder = Environment.GetEnvironmentVariable("DOCS_FOLDER") ?? DocsFolderLocator.Find();
Console.WriteLine($"{Tag} Docs folder = {docsFolder}, exists = {Directory.Exists(docsFolder)}");

var docsTools = new DocsTools(docsFolder);

IEmbeddingGenerator<string, Embedding<float>> embedder =
    new OllamaApiClient(new Uri(endpoint), "nomic-embed-text");
var indexer = new DocsIndexer(embedder, new TextChunker(500, 100), new InMemoryVectorStore());
await indexer.IndexFolderAsync(docsFolder);

var ragTools = new RagTools(indexer, 0.55f);
var ticketTools = new Tools.TicketTools(new Tools.InMemoryTicketStore());

IList<AITool> tools = new List<AITool>
{
    AIFunctionFactory.Create(docsTools.ListDocs),
    AIFunctionFactory.Create(ragTools.SearchDocs),
    AIFunctionFactory.Create(ticketTools.GetTicketStatus),
    AIFunctionFactory.Create(ticketTools.CalculateRefund)
};

string fileList = docsTools.ListDocs().Replace("\n", ", ");
IChatClient chatClient = ChatClientFactory.Create();
AIAgent agent = LocalAgentFactory.Create(chatClient, AgentPrompts.BuildDocsAssistant(fileList), tools);
IAgentRunner runner = new AgentRunnerAdapter(agent);
Console.WriteLine($"{Tag} Agent ready.");
// ---- Planner setup (same wiring as the console app) ----
var toolNames = new List<string> { "ListDocs", "SearchDocs", "GetTicketStatus", "CalculateRefund" };
IPlanGenerator planGenerator = new PlanGenerator(chatClient, toolNames, 5, 3);

// Final-answer agent has NO tools, so "Do not call tools" is actually enforced.
AIAgent finalAgent = LocalAgentFactory.Create(
    chatClient,
    "You write a final answer using only the step results you are given. Never ask for more data.",
    new List<AITool>());
IAgentRunner finalRunner = new AgentRunnerAdapter(finalAgent);

IPlanner planner = new PlannerService(planGenerator, runner, finalRunner);
Console.WriteLine($"{Tag} Planner ready.");
// Simple prompt-injection guard: length cap + suspicious phrases. Not bulletproof, just a first layer.
static string? CheckInput(string question)
{
    const int MaxLength = 500;
    string[] blocked =
    {
        "ignore previous instructions", "ignore all instructions", "reveal your system prompt",
        "show your system prompt", "you are now", "disregard the above"
    };

    if (question.Length > MaxLength)
    {
        return $"Question too long (max {MaxLength} chars).";
    }

    foreach (string phrase in blocked)
    {
        if (question.Contains(phrase, StringComparison.OrdinalIgnoreCase))
        {
            return "Question contains a blocked phrase.";
        }
    }

    return null;
}
app.MapPost("/plan", async (AskRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Question))
    {
        Console.WriteLine($"{Tag} /plan rejected: empty question");
        return Results.BadRequest(new { error = "Question is required." });
    }

    string? guardError = CheckInput(req.Question);
    if (guardError != null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{Tag} /plan BLOCKED by guard: {guardError}");
        Console.ResetColor();
        return Results.BadRequest(new { error = guardError });
    }

    var sw = Stopwatch.StartNew();
    Console.WriteLine($"{Tag} /plan start. Question length = {req.Question.Length}");

    try
    {
        var result = await planner.RunAsync(req.Question);
        sw.Stop();

        Console.ForegroundColor = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"{Tag} PLAN Success={result.Success}: {(result.Success ? result.FinalAnswer : result.Error)}");
        Console.ResetColor();
        Console.WriteLine($"{Tag} /plan done. LatencyMs = {sw.ElapsedMilliseconds}");

        return result.Success
            ? Results.Ok(new { answer = result.FinalAnswer, latencyMs = sw.ElapsedMilliseconds })
            : Results.Problem(result.Error, statusCode: 422);
    }
    catch (Exception ex)
    {
        sw.Stop();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{Tag} /plan ERROR after {sw.ElapsedMilliseconds} ms: {ex.Message}");
        Console.ResetColor();
        return Results.Problem("Planner failed.", statusCode: 500);
    }
}).RequireRateLimiting("ask");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/ask", async (AskRequest req, Microsoft.Extensions.Caching.Memory.IMemoryCache cache) =>
{
    if (string.IsNullOrWhiteSpace(req.Question))
    {
        Console.WriteLine($"{Tag} /ask rejected: empty question");
        return Results.BadRequest(new { error = "Question is required." });
    }

    string? guardError = CheckInput(req.Question);
    if (guardError != null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{Tag} /ask BLOCKED by guard: {guardError}");
        Console.ResetColor();
        return Results.BadRequest(new { error = guardError });
    }

    var sw = Stopwatch.StartNew();

    // Cache key: trimmed + lowercase, so "Hello" and " hello " share one entry.
    string cacheKey = req.Question.Trim().ToLowerInvariant();

    if (cache.TryGetValue(cacheKey, out string? cachedAnswer) && cachedAnswer != null)
    {
        sw.Stop();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{Tag} CACHE HIT. LatencyMs = {sw.ElapsedMilliseconds}");
        Console.ResetColor();
        return Results.Ok(new { answer = cachedAnswer, latencyMs = sw.ElapsedMilliseconds, cached = true });
    }

    Console.WriteLine($"{Tag} CACHE MISS. /ask start. Question length = {req.Question.Length}");

    try
    {
        string answer = await runner.AskAsync(req.Question);
        sw.Stop();

        // Cache only successful answers, for 5 minutes.
        cache.Set(cacheKey, answer, TimeSpan.FromMinutes(5));

        // Green = model answer, so it stands out from code logs.
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{Tag} ANSWER: {answer}");
        Console.ResetColor();
        Console.WriteLine($"{Tag} /ask done. LatencyMs = {sw.ElapsedMilliseconds}");

        return Results.Ok(new { answer, latencyMs = sw.ElapsedMilliseconds, cached = false });
    }
    catch (Exception ex)
    {
        sw.Stop();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{Tag} /ask ERROR after {sw.ElapsedMilliseconds} ms: {ex.Message}");
        Console.ResetColor();
        return Results.Problem("Agent failed.", statusCode: 500);
    }
}).RequireRateLimiting("ask");
app.Run();

public record AskRequest(string Question);