using System.ComponentModel;

namespace DocsAgent.Tools;

public class DocsTools
{
    private const string Tag = "[Tools/Docs]";
    private const int MaxChars = 8000;

    private readonly string _rootFolder;

    public DocsTools(string rootFolder)
    {
        _rootFolder = Path.GetFullPath(rootFolder);
        Console.WriteLine($"{Tag} Root folder = {_rootFolder}");
    }

    [Description("Lists the file names of all text documents that can be read.")]
    public string ListDocs()
    {
        Console.WriteLine($"{Tag} ListDocs called");

        if (!Directory.Exists(_rootFolder))
        {
            Console.WriteLine($"{Tag} ERROR: folder not found");
            return "Docs folder not found.";
        }

        string[] names = GetFileNames();
        Console.WriteLine($"{Tag} Found {names.Length} file(s)");
        return names.Length == 0 ? "No documents found." : string.Join("\n", names);
    }

    [Description("Reads the full text of one document. Use the exact file name from the available files list.")]
    public string ReadDoc([Description("File name only, without any folder path.")] string fileName)
    {
        Console.WriteLine($"{Tag} ReadDoc called with fileName='{fileName}'");

        // Security: keep only the file name so the model can never leave the docs folder.
        string safeName = Path.GetFileName(fileName ?? string.Empty);
        string fullPath = Path.Combine(_rootFolder, safeName);

        if (string.IsNullOrWhiteSpace(safeName) || !File.Exists(fullPath))
        {
            string available = string.Join(", ", GetFileNames());
            Console.WriteLine($"{Tag} File not found: '{safeName}'. Available: {available}");
            return $"File '{safeName}' was not found. The only valid file names are: {available}";
        }

        string text = File.ReadAllText(fullPath);
        Console.WriteLine($"{Tag} Read {text.Length} chars from {safeName}");

        if (text.Length > MaxChars)
        {
            Console.WriteLine($"{Tag} Truncating to {MaxChars} chars");
            text = text.Substring(0, MaxChars);
        }

        return text;
    }

    // Returns the names of all .txt files in the docs folder (empty array if the folder is missing).
    private string[] GetFileNames()
    {
        if (!Directory.Exists(_rootFolder))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(_rootFolder, "*.txt")
            .Select(p => Path.GetFileName(p))
            .ToArray();
    }
}