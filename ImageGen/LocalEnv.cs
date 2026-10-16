namespace ImageGen;

// Loads KEY=VALUE lines from an optional ".env.local" file into environment variables.
// A missing file is fine: the app just keeps using the real environment variables.
public static class LocalEnv
{
    private const string Tag = "[ImageGen/LocalEnv]";
    private const string FileName = ".env.local";

    public static void Load()
    {
        string? path = FindFile();
        if (path is null)
        {
            Console.WriteLine($"{Tag} No {FileName} found, using existing environment variables.");
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

            int index = line.IndexOf('=');
            if (index <= 0)
            {
                continue;
            }

            string name = line.Substring(0, index).Trim();
            string value = line.Substring(index + 1).Trim().Trim('"');

            // A real environment variable always wins over the file.
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
            {
                Environment.SetEnvironmentVariable(name, value);
                Console.WriteLine($"{Tag} Set {name}");
            }
        }
    }

    // Walks up from the exe folder (bin\Debug\net9.0) to the repo root looking for the file.
    private static string? FindFile()
    {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
