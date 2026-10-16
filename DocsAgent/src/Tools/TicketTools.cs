using System.ComponentModel;

namespace Tools;

// Minimal store abstraction so tools do not depend on fake data directly.
public interface ITicketStore
{
    bool TryGetStatus(string ticketId, out string status);
}

// Fake in-memory data, enough for eval cases.
public sealed class InMemoryTicketStore : ITicketStore
{
    private readonly Dictionary<string, string> _tickets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["T-1001"] = "open",
        ["T-1002"] = "in_progress",
        ["T-1003"] = "resolved",
        ["T-1004"] = "closed",
    };

    public bool TryGetStatus(string ticketId, out string status)
    {
        var found = _tickets.TryGetValue(ticketId.Trim(), out status!);
        Console.WriteLine($"[Tools/TicketStore] Lookup '{ticketId}' -> found={found}");
        return found;
    }
}

public sealed class TicketTools
{
    private readonly ITicketStore _store;

    public TicketTools(ITicketStore store)
    {
        _store = store;
    }

    [Description("Get the current status of a support ticket by its id, for example T-1001.")]
    public string GetTicketStatus(
        [Description("Ticket id like T-1001")] string ticketId)
    {
        Console.WriteLine($"[Tools/Tickets] GetTicketStatus called with ticketId='{ticketId}'");

        if (string.IsNullOrWhiteSpace(ticketId))
            return "ERROR: ticketId is empty. Ask the user for a ticket id like T-1001.";

        if (!_store.TryGetStatus(ticketId, out var status))
            return $"ERROR: ticket '{ticketId}' was not found. Valid ids look like T-1001.";

        return $"Ticket {ticketId} status: {status}";
    }

    [Description("Calculate the refund amount in USD for a subscription. Plan must be 'monthly' or 'annual'.")]
    public string CalculateRefund(
        [Description("Subscription plan: monthly or annual")] string plan,
        [Description("Number of days since the purchase, zero or more")] int daysSincePurchase)
    {
        Console.WriteLine($"[Tools/Tickets] CalculateRefund called with plan='{plan}', days={daysSincePurchase}");

        if (daysSincePurchase < 0)
            return "ERROR: daysSincePurchase cannot be negative.";

        decimal price;
        decimal percent;

        switch (plan?.Trim().ToLowerInvariant())
        {
            case "monthly":
                price = 10m;
                percent = daysSincePurchase <= 7 ? 1.0m : 0m;
                break;
            case "annual":
                price = 100m;
                percent = daysSincePurchase <= 14 ? 1.0m
                        : daysSincePurchase <= 30 ? 0.5m
                        : 0m;
                break;
            default:
                return $"ERROR: unknown plan '{plan}'. Use 'monthly' or 'annual'.";
        }

        var refund = price * percent;
        Console.WriteLine($"[Tools/Tickets] Refund computed: {refund} USD");
        return $"Refund for {plan} plan after {daysSincePurchase} days: {refund} USD";
    }
}