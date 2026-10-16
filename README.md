# DocsAgent

I built this to learn how the pieces of an AI app fit together in C#, without using a big framework to hide them. It's a console agent that answers questions about a folder of documents, plus a small ASP.NET Core API on top of it.

It runs locally on Ollama (llama3.2 for chat, nomic-embed-text for embeddings), so it costs nothing to run. Setting `LLM_PROVIDER=azure` switches the chat model to Azure OpenAI without code changes.

## What's in it

- **RAG.** The docs are split into overlapping chunks, embedded, and searched by meaning. Matches scoring below 0.55 are thrown away, and if nothing is left the agent says it doesn't know instead of guessing.
- **Tools.** The agent can call `ListDocs`, `SearchDocs`, `GetTicketStatus` and `CalculateRefund`. A tool that fails returns an error message to the model instead of throwing. I also capped each question at 3 model/tool round trips so it can't loop forever.
- **Structured output.** A `TicketExtractor` turns free text into a typed `TicketInfo`. If the JSON is invalid, the validation error goes back to the model and it tries again, up to 3 times. After that it fails cleanly.
- **Memory.** Three options: none, a sliding window of the last 20 messages, and a summary memory that compresses old messages.
- **Planner.** The model writes a JSON plan with at most 5 steps. I validate it (unknown tools are rejected, steps are renumbered) and retry if it's bad. Each step runs through the agent with the earlier results as context. The final answer comes from a second agent that has no tools at all, so it can't call one even if it wants to.
- **Evals.** Separate runners for RAG, tool choice, extraction and the planner. There's also a fake chat client I used to prove the retry path without needing a real model to misbehave on cue.
- **API.** `POST /ask`, `POST /plan` and `GET /health`, with rate limiting, a basic input guard, a response cache and latency logging.

```
Docs/*.txt -> chunk -> embed -> vector store            (once, at startup)
question -> embed -> top 3 chunks -> drop < 0.55 -> chat model -> answer
```

The sample documents are for a made-up company called FileNest (refunds, support hours, FAQ, past tickets). They're synthetic and written only for testing.

## Layout

```
DocsAgent/        console app: chat loop, eval commands, wiring
DocsAgent.Api/    minimal API
Planner/          plan generation, validation, execution
src/Shared        interfaces (IAgentRunner, IToolCallSource, ...)
src/Rag           chunker, vector store, indexer, SearchDocs
src/Tools         docs tools and ticket tools
src/Memory        none / sliding window / summary
src/Structured    typed extraction with retry
src/Evals         eval cases and runner
```

Modules talk through interfaces and every module logs with a tag like `[Tools/Rag]` or `[Planner/Run]`. I wanted to be able to debug a run from the console output alone.

## Running the console app

You need the .NET SDK and Ollama.

```bash
ollama pull llama3.2
ollama pull nomic-embed-text
dotnet run --project DocsAgent
```

Commands inside the app:

```
(any question)           full agent
/plan <question>         multi-step planner
/search <text>           retrieval only, shows top 3 chunks and scores
/eval                    RAG tests
/tools-eval              tool-choice tests
/extract-eval            structured extraction eval
/retry-test              retry path with a fake chat client
/plan-eval               planner eval
/memory on|off|summary   switch memory type
/clear                   reset memory
/exit                    quit
```

## Running the API

Start Ollama, set `DOCS_FOLDER` to the full path of the `Docs` folder (it's in `Properties/launchSettings.json`), and run `DocsAgent.Api`.

```bash
curl -X POST http://localhost:5201/ask \
  -H "Content-Type: application/json" \
  -d '{"question":"What is the status of ticket T-1002?"}'
```

- `/ask` has a 5 minute in-memory cache. A repeated question comes back in about 0 ms instead of a few seconds.
- Rate limit is 5 requests per 30 seconds per IP, using the built-in ASP.NET limiter.
- The input guard caps questions at 500 characters and blocks a short list of phrases like "ignore previous instructions". It's a first layer, not a real prompt-injection defense.

Other environment variables: `OLLAMA_ENDPOINT`, `OLLAMA_MODEL_NAME`, `LLM_PROVIDER`, and for Azure `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_KEY`, `AZURE_OPENAI_DEPLOYMENT`.

## Eval results

`/eval` has 10 cases: 7 answerable (the model must search and include the right facts), 2 unanswerable (it must admit the answer isn't in the docs) and 1 greeting (it must not search).

| Setup | Model | Score |
|---|---|---|
| Whole-file reading (`ReadDoc`) | llama3.2 | 5/10 |
| RAG, threshold 0.50 | llama3.2 | 8/10 |
| RAG, threshold 0.55 | llama3.2 | 9/10 |
| RAG | stronger model | TODO |

The first version let the model pick a file from a list. It guessed wrong for some questions and then answered confidently from the wrong file. Searching by meaning fixed all the answerable cases.

I tuned the threshold on the same 10 cases, so 9/10 is optimistic. The one failure left is the greeting: llama3.2 calls the search tool for "hello" even though the prompt tells it not to. Retrieval did its job there and dropped every chunk.

The other evals are noisier. Extraction with llama3.2 gave valid JSON 80% to 100% of the time and correct fields 60% to 73%, and the numbers moved between runs with identical code. For the planner, I only claim what I saw: it ran on multi-step questions, and the retry caught an invented tool name (`CompareDocs`) and produced a valid plan on the third attempt. The planner eval checks are loose keyword matches, so I'm not publishing a pass rate for it.

## Limitations

- llama3.2 ignores instructions fairly often. It calls tools for greetings, makes up ticket IDs, and sometimes sends a plan step to the wrong tool. The same question can produce different plans on different runs. I plan to rerun everything with a stronger model.
- Chunking is character-based, so it can cut a word in half.
- Only `.txt` files are supported. The vector store, the ticket store and the cache are all in memory.
- The 0.55 threshold fits `nomic-embed-text` on this small corpus and would need re-tuning for other models or data.
- The evals check that the needed facts appear in the answer. They don't judge how well the answer is written.
- The API has no authentication.

## Next

- Run the evals with a stronger model and fill in the last row of the table.
- Expose the tools through an MCP server.
