namespace DocsAgent.Evals;

public enum EvalKind
{
    Answerable,     // answer exists in the docs; model must read the file and answer correctly
    Unanswerable,   // answer is NOT in the docs; model must say it could not find it
    NoToolNeeded    // greeting / small talk; model must NOT read any document
}

public sealed record EvalCase(
    string Id,
    EvalKind Kind,
    string Question,
    string[]? MustContainAll = null);


