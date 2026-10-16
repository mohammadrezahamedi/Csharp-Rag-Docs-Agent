namespace Structured;

/// <summary>Structured ticket extracted from raw support text.</summary>
public sealed record TicketInfo(
    string Category,      // one of: billing, technical, account, other
    string Urgency,       // one of: low, medium, high
    string Summary,       // max ~200 chars
    bool MentionsRefund);