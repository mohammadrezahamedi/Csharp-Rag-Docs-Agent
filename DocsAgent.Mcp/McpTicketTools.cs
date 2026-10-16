using System.ComponentModel;
using ModelContextProtocol.Server;
using Tools;

namespace McpServerApp;

// Thin MCP adapter: exposes TicketTools methods as MCP tools.
[McpServerToolType]
public sealed class McpTicketTools
{
    private readonly TicketTools _inner;

    public McpTicketTools(TicketTools inner)
    {
        _inner = inner;
    }

    [McpServerTool(Name = "GetTicketStatus"),
     Description("Get the current status of a support ticket by its id, for example T-1001.")]
    public string GetTicketStatus(
        [Description("Ticket id like T-1001")] string ticketId)
    {
        Console.WriteLine($"[Mcp/Tickets] GetTicketStatus ticketId='{ticketId}'");
        var result = _inner.GetTicketStatus(ticketId);
        Console.WriteLine($"[Mcp/Tickets] Result: {result}");
        return result;
    }

    [McpServerTool(Name = "CalculateRefund"),
     Description("Calculate the refund amount in USD for a subscription. Plan must be 'monthly' or 'annual'.")]
    public string CalculateRefund(
        [Description("Subscription plan: monthly or annual")] string plan,
        [Description("Number of days since the purchase, zero or more")] int daysSincePurchase)
    {
        Console.WriteLine($"[Mcp/Tickets] CalculateRefund plan='{plan}', days={daysSincePurchase}");
        var result = _inner.CalculateRefund(plan, daysSincePurchase);
        Console.WriteLine($"[Mcp/Tickets] Result: {result}");
        return result;
    }
}