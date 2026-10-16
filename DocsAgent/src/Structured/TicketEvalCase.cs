namespace Structured;

/// <summary>One test input with the values we expect the model to extract.</summary>
public sealed record TicketEvalCase(
    string Input,
    string ExpectedCategory,
    string ExpectedUrgency,
    bool ExpectedMentionsRefund);