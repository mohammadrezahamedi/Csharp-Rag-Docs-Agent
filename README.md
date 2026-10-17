# DocsAgent

C# AI agent that answers questions over a docs folder (support tickets, FAQ, refunds). Built with Microsoft Agent Framework + Microsoft.Extensions.AI. Runs free on local Ollama, or on Azure OpenAI.

## Features

- **Tools**: the agent calls tools (list docs, read doc, search) with a hard cap on tool iterations
- **RAG**: document chunking, embeddings, cosine search with a minimum score
- **Memory**: conversation memory with automatic summarization
- **Planner**: breaks a goal into steps
- **Structured output**: ticket extraction to typed JSON with retry on invalid output
- **Evals**: every case runs 5 times (stochastic testing), reports valid / perfect / per-field rates and flags flaky cases
- **Tracing**: every model call and tool call is written to `traces/*.jsonl` with timing, plus colored console output
- **Provider switch**: `LLM_PROVIDER=ollama|azure` via `.env.local`

## Run

```
copy .env.example .env.local
dotnet run --project DocsAgent/DocsAgent.csproj
```

Commands inside the app: `/eval`, `/search <text>`, `/memory on|off`, `/clear`, `/exit`
# DocsAgent

C# console AI agent with tool calling, RAG, structured extraction, planner, MCP server and evals. Runs fully local on Ollama (free).

## Run

```
copy .env.example .env.local
dotnet run --project DocsAgent/DocsAgent.csproj
```

## Modules
- Rag: document chunking, indexing, retrieval
- Tools: function calling
- Structured: ticket extraction with retry and evals
- Planner: multi-step plan generation
- Memory: conversation memory
- Evals: eval cases and runner
- DocsAgent.Mcp: MCP server exposing ticket tools

## Notes
- Built on .NET 9
- Local models are weaker than hosted ones, so some eval failures come from the model, not the code
- No API keys needed. If you add a hosted provider, put the key in appsettings.Local.json or an environment variable (both are git-ignored)

## Setup (Ollama models, one time)

If you see a 404 from Ollama at startup, an embedding model is missing: run the pull above.


