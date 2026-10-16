# DocsAgent

A C# / .NET 9 AI-engineering portfolio: one console agent plus ten focused projects (memory, structured output, tool calling, RAG, evals, planner, multimodal, REST API, guardrails, MCP). It runs locally on Ollama and has an Azure OpenAI path for stronger models.

## What is inside

| # | Project | Where | What it shows |
|---|---------|-------|---------------|
| 1 | Memory | `DocsAgent/src/Memory` | Sliding-window memory and summary memory behind one interface |
| 2 | Structured output | `DocsAgent/src/Structured` | Typed ticket extraction with validation and retry |
| 3 | Tool calling | `DocsAgent/src/Tools` | Ticket status, refund calculator, document listing |
| 4 | RAG | `DocsAgent/src/Rag` | Chunking, embeddings (`nomic-embed-text`), in-memory vector store, score threshold and top-K |
| 5 | Evals | `DocsAgent/src/Evals` | Eval runners and suites for answers and tool calls |
| 6 | Planner | `Planner` | Plan generation, step execution, final-answer agent without tools |
| 7 | Multimodal | `Vision` | Image + question sent to a local vision model |
| 8 | REST API | `DocsAgent.Api` | Minimal API on top of the agent |
| 9 | Guardrails | `DocsAgent.Api` | Implemented inside the API project |
| 10 | MCP server | `DocsAgent.Mcp` | stdio MCP server exposing the ticket tools |
| + | Image generation | `ImageGen` | Text-to-image, seeded tweaks and remix behind provider-agnostic interfaces |

## Design notes

- Every module is a small class behind an interface; `Program.cs` only wires them together.
- Providers are chosen by factories and environment variables (`ChatClientFactory`, `ImageCreatorFactory`), so the same code runs on Ollama or Azure OpenAI.
- Logs carry a module tag such as `[Rag/Indexer]` or `[Vision/Describer]`. Model answers print in green, code logs stay in the default color.
- Built on `Microsoft.Extensions.AI`, `Microsoft.Agents.AI`, `OllamaSharp`, `ModelContextProtocol` and `Azure.AI.OpenAI`.

## Quick start

Requirements: .NET 9 SDK and [Ollama](https://ollama.com).

```powershell
ollama pull llama3.2
ollama pull nomic-embed-text
ollama pull qwen3-vl:8b
dotnet run --project DocsAgent/DocsAgent.csproj
```

Stop any running instance of the app before building, otherwise the locked executable makes the build fail.

### Configuration

| Variable | Default | Purpose |
|----------|---------|---------|
| `LLM_PROVIDER` | `ollama` | `ollama` or `azure` |
| `OLLAMA_ENDPOINT` | `http://localhost:11434` | Ollama server |
| `OLLAMA_MODEL_NAME` | `llama3.2` | Chat model |
| `OLLAMA_VISION_MODEL_NAME` | `qwen3-vl:8b` | Vision model |
| `DOCS_FOLDER` | auto-detected | Folder with the documents to index |
| `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_KEY`, `AZURE_OPENAI_DEPLOYMENT` | none | Azure chat model |
| `IMAGE_PROVIDER` | `azure` | `azure` or `pollinations` |
| `AZURE_OPENAI_IMAGE_DEPLOYMENT` | none | Azure image deployment |
| `POLLINATIONS_API_KEY` | none | Pollinations key |
| `POLLINATIONS_MODEL` | `flux` | Text-to-image model |

Secrets can live in a git-ignored `.env.local` at the repo root (see `.env.example`). Real environment variables always win over the file, and a missing file is fine.

## Console commands

```
(any text)                 ask the agent (tools + RAG)
/search <text>
/eval
/tools-eval
/extract-eval
/plan <text>
/plan-eval
/vision <image path> | <question>
/generate <prompt>
/imagine <prompt>
/tweak <change>
/remix <image path> | <optional style>
/memory on
/memory off
/memory summary
/clear
/retry-test
/exit
```

## MCP server

`DocsAgent.Mcp` is a stdio MCP server (`ModelContextProtocol` 2.2.0) with two tools, `GetTicketStatus` and `CalculateRefund`. It is a thin adapter over the same `TicketTools` the agent uses. Stdout is reserved for JSON-RPC, so console output is redirected to stderr.

Test it with the MCP Inspector (on Windows PowerShell use `npx.cmd` if script execution is restricted):

```powershell
npx @modelcontextprotocol/inspector dotnet run --project DocsAgent.Mcp/DocsAgent.Mcp.csproj
```

Verified in the Inspector: `tools/list` works, `GetTicketStatus` for `T-1001` returns `open`, and `CalculateRefund` for an annual plan after 30 days returns `50.0 USD`.

## REST API

`DocsAgent.Api` is a minimal API over the agent (http `5201`, https `7289` in the launch profile).

<!-- TODO: list the real endpoints from DocsAgent.Api/Program.cs and confirm /health in a browser before publishing -->

## Vision

`/vision <image path> | <question>` sends an image and a question to a local vision model (`qwen3-vl:8b` through Ollama) using `DataContent` + `TextContent` from `Microsoft.Extensions.AI`.

```
/vision samples/invoice.png | What text do you see in this image?
```

## Image generation

`ImageGen` keeps image providers behind interfaces (`IImageCreator`, `ISeededImageCreator`).

- `/generate` makes an image from text.
- `/imagine` + `/tweak` make a base image, then re-render it with an extra change and the same seed. This is not pixel editing: composition usually stays similar, details change.
- `/remix` lets the vision model describe an image, then generates a new one from that description.

Status: `/generate`, `/imagine`, `/tweak` and `/remix` were run end to end with Pollinations. The Azure OpenAI image provider builds but has not been run against a real deployment.


Privacy: with the `pollinations` provider, prompts leave your machine.

## Results and known limitations

Development used a small local model (`llama3.2`), which is fast and free but weak. These issues come from the model, not from the code, and were not worked around:

- extra or irrelevant plan steps and the wrong tool chosen for a step
- invented arguments or documents
- answer prefixes such as "The final answer is:" and tool calls for plain greetings
- vision answers that misread small details

| Eval | Strong-model result |
|------|---------------------|
| `/eval` (RAG + agent) | _pending final run_ |
| `/tools-eval` | _pending final run_ |
| `/plan-eval` | _pending final run_ |
| `/extract-eval` | _pending final run_ |

<!-- TODO: fill the table after running the evals once with a strong model (Azure OpenAI or similar) -->

## Repository layout

```
DocsAgent/            console app, Agents/, Prompts/, Docs/, src/ (Evals, Memory, Rag, Shared, Structured, Tools)
DocsAgent.Api/        minimal API
DocsAgent.Mcp/        MCP server (kept outside DocsAgent/ so its files are not compiled into the console app)
Planner/              planner
Vision/               image description
ImageGen/             image generation, tweak, remix
samples/              small sample images
```
