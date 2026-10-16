namespace DocsAgent.Tools;

// Lets other modules (like the eval runner) observe tool calls without knowing the tool class.
public interface IToolCallSource
{
    event Action<string>? ToolCalled;
}