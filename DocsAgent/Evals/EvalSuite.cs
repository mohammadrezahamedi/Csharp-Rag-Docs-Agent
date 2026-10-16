namespace DocsAgent.Evals;

// All eval questions live here, separate from the runner logic.
public static class EvalSuite
{
    public static IReadOnlyList<EvalCase> Default()
    {
        return new List<EvalCase>
        {
            new EvalCase("players", EvalKind.Answerable,
                "How many players does MPKit support per room?",
                new[] { "8" }),

            new EvalCase("tick-and-lib", EvalKind.Answerable,
                "What is the default tick rate and which networking library does it use?",
                new[] { "60", "fusion" }),

            new EvalCase("quote", EvalKind.Answerable,
                "Quote the exact text of the document.",
                new[] { "multiplayer kit", "photon fusion 2" }),

            new EvalCase("admin-password", EvalKind.Unanswerable,
                "What is the default password of the admin panel?"),

            new EvalCase("hello", EvalKind.NoToolNeeded,
                "hello")
        };
    }
}