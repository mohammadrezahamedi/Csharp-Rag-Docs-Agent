using System.Diagnostics;
using System.Text.Json;

namespace DocsAgent.Shared;

/// <summary>Records agent steps (model calls, tool calls, errors) for later inspection.</summary>
public interface ITracer
{
    void Record(string kind, string name, string? input, string? output, long elapsedMs);
    Task<T> MeasureAsync<T>(string kind, string name, string? input, Func<Task<T>> action, Func<T, string?>? describe = null);
}

/// <summary>Does nothing. Default when tracing is not wired.</summary>
public sealed class NullTracer : ITracer
{
    public void Record(string kind, string name, string? input, string? output, long elapsedMs) { }
    public Task<T> MeasureAsync<T>(string kind, string name, string? input, Func<Task<T>> action, Func<T, string?>? describe = null) => action();
}

/// <summary>Writes one JSON line per step to traces/*.jsonl and prints colored lines to the console.</summary>
public sealed class JsonlTracer : ITracer
{
    private readonly string _path;
    private readonly object _lock = new();

    public JsonlTracer(string? folder = null)
    {
        folder ??= Path.Combine(AppContext.BaseDirectory, "traces");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd-HHmmss}.jsonl");
        Console.WriteLine($"[Trace] writing to {_path}");
    }

    public void Record(string kind, string name, string? input, string? output, long elapsedMs)
    {
        var entry = new { time = DateTime.Now, kind, name, input, output, ms = elapsedMs };
        lock (_lock) File.AppendAllText(_path, JsonSerializer.Serialize(entry) + Environment.NewLine);

        // model = cyan, tool = magenta, error = red, rest = gray
        Console.ForegroundColor = kind switch
        {
            "model" => ConsoleColor.Cyan,
            "tool" => ConsoleColor.Magenta,
            "error" => ConsoleColor.Red,
            _ => ConsoleColor.DarkGray
        };
        Console.WriteLine($"[Trace/{kind}] {name} ({elapsedMs} ms) in: {Cut(input)} | out: {Cut(output)}");
        Console.ResetColor();
    }

    public async Task<T> MeasureAsync<T>(string kind, string name, string? input, Func<Task<T>> action, Func<T, string?>? describe = null)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            Record(kind, name, input, describe?.Invoke(result) ?? result?.ToString(), sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            Record("error", name, input, ex.Message, sw.ElapsedMilliseconds);
            throw;
        }
    }

    private static string Cut(string? s) => s == null ? "-" : (s.Length <= 120 ? s : s[..120] + "...").Replace("\n", " ");
}

