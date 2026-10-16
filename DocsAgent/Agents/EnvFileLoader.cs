namespace DocsAgent.Agents;

// Loads KEY=VALUE pairs from a git-ignored ".env.local" into process env vars (the file wins over system vars).
public static class EnvFileLoader
{
    private const string Tag = "[Agents/EnvFile]";

    public static void Load()
    {
        string? path = FindFile(AppContext.BaseDirectory) ?? FindFile(Directory.GetCurrentDirectory());
        if (path == null)
        {
            Console.WriteLine($"{Tag} No .env.local found. Using system environment variables only.");
            return;
        }

        Console.WriteLine($"{Tag} Loading {path}");
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            string name = line[..eq].Trim();
            string value = line[(eq + 1)..].Trim().Trim('"');
            Environment.SetEnvironmentVariable(name, value);
            Console.WriteLine($"{Tag} Set {name} (value hidden)");
        }
    }

    // Walks up from the start folder so it works from bin/Debug and from the repo root.
    private static string? FindFile(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, ".env.local");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
