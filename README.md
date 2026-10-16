# DocsAgent

C# console AI agent with tool calling, RAG, structured extraction, planner, MCP server and evals. Runs fully local on Ollama (free).

## Run

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
