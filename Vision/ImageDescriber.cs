using Microsoft.Extensions.AI;

namespace DocsAgent.Vision;

public sealed class ImageDescriber : IImageDescriber
{
    private const string Tag = "[Vision/Describer]";
    private readonly IChatClient _client;

    public ImageDescriber(IChatClient client)
    {
        _client = client;
    }

    public async Task<string> DescribeAsync(string imagePath, string question, CancellationToken ct = default)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"{Tag} Image not found.", imagePath);
        }

        byte[] bytes = await File.ReadAllBytesAsync(imagePath, ct);
        string mime = GetMimeType(imagePath);
        Console.WriteLine($"{Tag} Image={Path.GetFileName(imagePath)}, Bytes={bytes.Length}, Mime={mime}");
        Console.WriteLine($"{Tag} Question={question}");

        var message = new ChatMessage(ChatRole.User, new List<AIContent>
        {
            new TextContent(question),
            new DataContent(bytes, mime)
        });

        ChatResponse response = await _client.GetResponseAsync(message, cancellationToken: ct);
        Console.WriteLine($"{Tag} Got answer. Length={response.Text.Length}");
        return response.Text;
    }

    private static string GetMimeType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => throw new NotSupportedException($"{Tag} Unsupported image type. Use png, jpg or webp.")
        };
    }
}