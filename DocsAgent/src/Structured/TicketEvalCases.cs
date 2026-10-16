namespace Structured;

/// <summary>Small fixed test set for the ticket extractor.</summary>
public static class TicketEvalCases
{
    public static readonly IReadOnlyList<TicketEvalCase> All = new List<TicketEvalCase>
    {
        new("I was charged twice for my Pro plan and the app crashes on login. Fix this today and refund the extra charge!",
            "billing", "high", true),
        new("The app crashes every time I upload a file larger than 1GB. No rush, whenever you can.",
            "technical", "low", false),
        new("URGENT: I am locked out of my account and I have a client demo in one hour!",
            "account", "high", false),
        new("Hi, can you tell me what your support office hours are?",
            "other", "low", false),
        new("I never used my annual plan. When you get a chance, I would like my money back.",
            "billing", "low", true),
    };
}