# DocsAgent

A small C# console agent that answers questions about a folder of documents using **RAG** (retrieval-augmented generation), tool calling, and an automated **evaluation harness**. It runs fully local and free with [Ollama](https://ollama.com).

## What it does

1. Reads `.txt` files from the `Docs` folder.
2. Splits them into overlapping chunks and embeds each chunk (`nomic-embed-text`).
3. When you ask a question, the agent calls a `SearchDocs` tool that finds the closest chunks by meaning, drops weak matches (score below 0.55), and passes the rest to the chat model.
4. If nothing relevant is found, the agent says so instead of guessing.

```
Docs/*.txt -> chunk -> embed -> vector store            (once, at startup)
question -> embed -> top 3 chunks -> drop < 0.55 -> chat model -> answer
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
| `/search <text>` | Retrieval only: shows the top 3 chunks and their scores (for debugging) |
| `/eval` | Runs the automated quality tests |
| `/clear` | Clears conversation memory |
| `/exit` | Quit |

Optional environment variables: `OLLAMA_ENDPOINT`, `OLLAMA_MODEL_NAME`, `DOCS_FOLDER`.

## Evaluation

`/eval` runs 10 cases of three kinds:

- **Answerable**: the model must call the search tool and include the required facts in its answer.
- **Unanswerable**: the model must admit the answer is not in the documents.
- **NoToolNeeded**: small talk must not trigger a search.

| Setup | Model | Score |
|---|---|---|
| Whole-file reading (`ReadDoc`) | llama3.2 | 5/10 |
| RAG (`SearchDocs`, threshold 0.50) | llama3.2 | 8/10 |
| RAG (`SearchDocs`, threshold 0.55) | llama3.2 | 9/10 |
| RAG (`SearchDocs`) | stronger model | TODO |

What the numbers show:

- The `ReadDoc` baseline had to guess a file name from a list, opened the wrong file for some questions, and answered confidently from it.
- With RAG, all 7 answerable cases pass because the right chunk is found by meaning, not by a guessed file name.
- The threshold was tuned on this same 10-case set, so the 9/10 is optimistic.
- The remaining failure (`hello`) is a small-model issue: llama3.2 calls the search tool for a greeting even though the prompt says not to. Retrieval itself behaved correctly (all chunks were dropped).

## Design notes

- Modules are decoupled through interfaces (`ITextChunker`, `IVectorStore`, `IDocsSearcher`, `IToolCallSource`), so the in-memory vector store can be replaced without touching the agent.
- Tool calls are observable through an event, which is how the eval runner checks that the right tool was used.
- Every module logs with a tag prefix (for example `[Tools/Rag]`), so a run can be debugged from the console alone.
- File access is restricted to the docs folder (file names only, no paths).
- The agent is instructed that it can only explain, not perform actions such as refunds, and must say so when asked.

## Known limitations

- Small local models (llama3.2) sometimes ignore instructions, for example calling a tool for a greeting. Results with a stronger model are listed above.
- Chunking is character-based and can cut a word in the middle.
- Only `.txt` files are supported, and the vector store is in memory.
- The score threshold (0.55) is tuned for `nomic-embed-text` on this small corpus and should be re-tuned for other models or data.
- The eval checks that required facts appear in the answer, not that the answer is well written. A pass means "the needed facts are present".

## Next steps

- Run the eval with a stronger model and fill in the last table row.
- Expose the agent through an ASP.NET Core API.
