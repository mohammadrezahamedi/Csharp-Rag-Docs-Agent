using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Tools;

// stdout is reserved for MCP JSON-RPC, so all Console.WriteLine output goes to stderr.
Console.SetOut(Console.Error);
Console.WriteLine("[Mcp/Main] Starting DocsAgent MCP server (stdio)");

var builder = Host.CreateApplicationBuilder(args);

// Host logs also go to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Reuse the existing tools from DocsAgent, no duplicated logic.
builder.Services.AddSingleton<ITicketStore, InMemoryTicketStore>();
builder.Services.AddSingleton<TicketTools>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

Console.WriteLine("[Mcp/Main] Server configured, waiting for client");
await builder.Build().RunAsync();