namespace DocsAgent.Tools;

// Finds the Docs folder without a hardcoded drive path, so the repo runs on any machine.
public static class DocsFolderLocator
{
    private const string Tag = "[Tools/Locator]";

    public static string Find()
    {
        // 1) Explicit override always wins.
        string? fromEnv = Environment.GetEnvironmentVariable("DOCS_FOLDER");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            Console.WriteLine($"{Tag} Using DOCS_FOLDER = {fromEnv}");
            return fromEnv;
        }

        // 2) Walk up from the exe folder until a folder named "Docs" is found.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "Docs");
            if (Directory.Exists(candidate))
            {
                Console.WriteLine($"{Tag} Found Docs folder at {candidate}");
                return candidate;
            }

            dir = dir.Parent;
        }

        // 3) Last resort: current working directory.
        string fallback = Path.Combine(Directory.GetCurrentDirectory(), "Docs");
        Console.WriteLine($"{Tag} Docs folder not found while walking up. Falling back to {fallback}");
        return fallback;
    }
}
