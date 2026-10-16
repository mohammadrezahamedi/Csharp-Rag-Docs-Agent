namespace DocsAgent.Evals;

// All eval questions live here, separate from the runner logic.
public static class EvalSuite
{
    public static IReadOnlyList<EvalCase> Default()
    {
        return new List<EvalCase>
        {
            // ---- Answerable: the answer exists in the FileNest docs ----
            new EvalCase("refund-yearly", EvalKind.Answerable,
                "How many days do I have to ask for a refund on a yearly plan?",
                new[] { "30" }),

            // Same fact, different wording: tests that search works on meaning.
            new EvalCase("refund-yearly-paraphrase", EvalKind.Answerable,
                "Within how many days can I get my money back if I pay once a year?",
                new[] { "30" }),

            new EvalCase("refund-monthly", EvalKind.Answerable,
                "How many days do I have to get a refund on a monthly plan?",
                new[] { "7" }),

            new EvalCase("phone-support", EvalKind.Answerable,
                "Can I call support on the phone?",
                new[] { "business" }),

            new EvalCase("restore-file", EvalKind.Answerable,
                "I deleted a file by mistake, can I get it back?",
                new[] { "30", "trash" }),

            new EvalCase("pro-price", EvalKind.Answerable,
                "How much does the Pro plan cost per month?",
                new[] { "9" }),

            new EvalCase("iphone-refund", EvalKind.Answerable,
                "I bought Pro inside the iPhone app. Can you refund it?",
                new[] { "apple" }),

            // ---- Unanswerable: the answer is NOT in the docs ----
            new EvalCase("student-discount", EvalKind.Unanswerable,
                "Do you have a student discount?"),

            new EvalCase("free-trial", EvalKind.Unanswerable,
                "Do you offer a free trial of the Business plan?"),

            // ---- NoToolNeeded: small talk ----
            new EvalCase("hello", EvalKind.NoToolNeeded,
                "hello")
        };
    }
}
