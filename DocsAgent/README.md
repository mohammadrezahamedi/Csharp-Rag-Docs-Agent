# DocsAgent

A small C# console agent that answers questions about a folder of documents using **RAG** (retrieval-augmented generation), tool calling, and an automated **evaluation harness**. Runs fully local and free with [Ollama](https://ollama.com).

## What it does

1. Reads `.txt` files from the `Docs` folder.
2. Splits them into overlapping chunks and embeds each chunk (`nomic-embed-text`).
3. When you ask a question, the agent calls a `SearchDocs` tool that finds the closest chunks by meaning, drops weak matches (score below 0.5), and passes the rest to the chat model.
4. If nothing relevant is found, the agent says so instead of guessing.

```
Docs/*.txt -> chunk -> embed -> vector store            (once, at startup)
question -> embed -> top 3 chunks -> drop < 0.5 -> chat model -> answer
```

## Sample data

The `Docs` folder contains documents for a fictional company called **FileNest** (refund rules, support hours, FAQ, past tickets). **Sample documents are synthetic, created for testing.**

## Run it

Requirements: .NET SDK (see the `.csproj`) and Ollama.

```bash
ollama pull llama3.2
ollama pull nomic-embed-text
dotnet run
```

Commands inside the app:

| Command | What it does |
|---|---|
| (any question) | Full agent: search, filter, answer |
| `/search <text>` | Retrieval only: shows top 3 chunks and scores (for debugging) |
| `/eval` | Runs the automated quality tests |
| `/clear` | Clears conversation memory |
| `/exit` | Quit |

Optional environment variables: `OLLAMA_ENDPOINT`, `OLLAMA_MODEL_NAME`, `DOCS_FOLDER`.

## Evaluation

`/eval` runs 10 cases in three kinds:

- **Answerable**: the model must call the search tool and include the required facts.
- **Unanswerable**: the model must admit the answer is not in the documents.
- **NoToolNeeded**: small talk must not trigger a search.

| Setup | Model | Score |
|---|---|---|
| Whole-file reading (`ReadDoc`) | llama3.2 | 5/10 |
| RAG (`SearchDocs`) | llama3.2 | TODO |
| RAG (`SearchDocs`) | stronger model | TODO |

The `ReadDoc` baseline showed the problem RAG solves: the model had to guess a file name from a list, opened the wrong file for some questions, and answered confidently from it.

## Design notes

- Modules are decoupled through interfaces (`ITextChunker`, `IVectorStore`, `IDocsSearcher`, `IToolCallSource`), so the in-memory vector store can be replaced without touching the agent.
- Tool calls are observable through an event, which is how the eval runner checks that the right tool was used.
- Every module logs with a tag prefix (for example `[Tools/Rag]`) so a run can be debugged from the console alone.
- File access is restricted to the docs folder (file names only, no paths).

## Known limitations

- Small local models (llama3.2) sometimes ignore instructions, for example calling a tool for a greeting. Results with a stronger model are listed above.
- Chunking is character-based and can cut a word in the middle.
- Only `.txt` files are supported; the vector store is in memory.
- The 0.5 score threshold is tuned for `nomic-embed-text` on this small corpus and should be re-tuned for other models or data.
