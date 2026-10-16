using System.Net.Http.Headers;

namespace ImageGen;

// HTTP image creator for Pollinations (gen.pollinations.ai), with an optional fixed seed.
public sealed class HttpImageCreator : ISeededImageCreator
{
    private const string Tag = "[ImageGen/Http]";
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly string? _apiKey;

    public HttpImageCreator(HttpClient http, string baseUrl, string model, string? apiKey)
    {
        _http = http;
        _baseUrl = baseUrl;
        _model = model;
        _apiKey = apiKey;
    }

    public Task<string> CreateAsync(string prompt, string outputPath, CancellationToken ct = default)
    {
        return DownloadAsync(prompt, outputPath, null, ct);
    }

    public Task<string> CreateAsync(string prompt, string outputPath, int seed, CancellationToken ct = default)
    {
        return DownloadAsync(prompt, outputPath, seed, ct);
    }

    private async Task<string> DownloadAsync(string prompt, string outputPath, int? seed, CancellationToken ct)
    {
        string seedPart = seed.HasValue ? $"&seed={seed.Value}" : string.Empty;
        string url = $"{_baseUrl.TrimEnd('/')}/image/{Uri.EscapeDataString(prompt)}?model={Uri.EscapeDataString(_model)}{seedPart}";
        Console.WriteLine($"{Tag} GET {url}, Seed={(seed.HasValue ? seed.Value.ToString() : "random")}, ApiKey={(string.IsNullOrWhiteSpace(_apiKey) ? "none" : "***")}");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        using HttpResponseMessage response = await _http.SendAsync(request, ct);
        string mime = response.Content.Headers.ContentType?.MediaType ?? "unknown";
        Console.WriteLine($"{Tag} Status={(int)response.StatusCode}, Mime={mime}");

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(ct);
            string shortBody = body.Length > 200 ? body.Substring(0, 200) : body;
            throw new InvalidOperationException($"{Tag} HTTP {(int)response.StatusCode}: {shortBody}");
        }

        if (!mime.StartsWith("image/"))
        {
            throw new InvalidOperationException($"{Tag} Response is not an image (Mime={mime}).");
        }

        byte[] bytes = await response.Content.ReadAsByteArrayAsync(ct);

        // The service may answer with jpeg or png, so the extension follows the real mime type.
        string extension = mime switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            _ => ".img"
        };
        string finalPath = Path.ChangeExtension(outputPath, extension);

        string? folder = Path.GetDirectoryName(finalPath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        await File.WriteAllBytesAsync(finalPath, bytes, ct);
        Console.WriteLine($"{Tag} Saved. Bytes={bytes.Length}, Path={finalPath}");
        return finalPath;
    }
}
