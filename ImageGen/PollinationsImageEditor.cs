using System.Net.Http.Headers;
using System.Text.Json;

namespace ImageGen;

// Upload local image -> edit through a public URL (kontext) -> save result -> delete the upload.
public sealed class PollinationsImageEditor : IImageEditor
{
    private const string Tag = "[ImageGen/Edit]";
    private readonly HttpClient _http;
    private readonly string _genUrl;
    private readonly string _mediaUrl;
    private readonly string _model;
    private readonly string _apiKey;

    public PollinationsImageEditor(HttpClient http, string genUrl, string mediaUrl, string model, string apiKey)
    {
        _http = http;
        _genUrl = genUrl;
        _mediaUrl = mediaUrl;
        _model = model;
        _apiKey = apiKey;
    }

    public async Task<string> EditAsync(string sourceImagePath, string instruction, string outputPath, CancellationToken ct = default)
    {
        if (!File.Exists(sourceImagePath))
        {
            throw new FileNotFoundException($"{Tag} Source image not found.", sourceImagePath);
        }

        byte[] source = await File.ReadAllBytesAsync(sourceImagePath, ct);
        string sourceMime = GetMime(sourceImagePath);
        Console.WriteLine($"{Tag} Source={Path.GetFileName(sourceImagePath)}, Bytes={source.Length}, Model={_model}");
        Console.WriteLine($"{Tag} Instruction={instruction}");

        (string url, string id) = await UploadAsync(source, sourceMime, ct);
        try
        {
            return await EditRemoteAsync(url, instruction, outputPath, ct);
        }
        finally
        {
            // The upload gets a public link, so it is removed right after use (best effort).
            await TryDeleteAsync(id);
        }
    }

    private async Task<(string Url, string Id)> UploadAsync(byte[] bytes, string mime, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_mediaUrl.TrimEnd('/')}/upload");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new ByteArrayContent(bytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);

        using HttpResponseMessage response = await _http.SendAsync(request, ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        Console.WriteLine($"{Tag} Upload status={(int)response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{Tag} Upload failed: HTTP {(int)response.StatusCode} {Shorten(body)}");
        }

        string? url = null;
        string? id = null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("url", out JsonElement urlElement))
            {
                url = urlElement.GetString();
            }

            if (doc.RootElement.TryGetProperty("id", out JsonElement idElement))
            {
                id = idElement.GetString();
            }
        }
        catch (JsonException)
        {
            // Some responses are just the plain url.
            url = body.Trim();
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException($"{Tag} Upload response has no url: {Shorten(body)}");
        }

        id ??= url.Substring(url.LastIndexOf('/') + 1);
        Console.WriteLine($"{Tag} Uploaded. Id={id}");
        return (url, id);
    }

    private async Task<string> EditRemoteAsync(string imageUrl, string instruction, string outputPath, CancellationToken ct)
    {
        string requestUrl = $"{_genUrl.TrimEnd('/')}/image/{Uri.EscapeDataString(instruction)}" +
                            $"?model={Uri.EscapeDataString(_model)}&image={Uri.EscapeDataString(imageUrl)}";
        Console.WriteLine($"{Tag} Requesting edit with model {_model}");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using HttpResponseMessage response = await _http.SendAsync(request, ct);
        string mime = response.Content.Headers.ContentType?.MediaType ?? "unknown";
        Console.WriteLine($"{Tag} Edit status={(int)response.StatusCode}, Mime={mime}");

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"{Tag} Edit failed: HTTP {(int)response.StatusCode} {Shorten(body)}");
        }

        if (!mime.StartsWith("image/"))
        {
            throw new InvalidOperationException($"{Tag} Response is not an image (Mime={mime}).");
        }

        byte[] bytes = await response.Content.ReadAsByteArrayAsync(ct);
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

    private async Task TryDeleteAsync(string id)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{_mediaUrl.TrimEnd('/')}/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            using HttpResponseMessage response = await _http.SendAsync(request);
            Console.WriteLine($"{Tag} Cleanup delete status={(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{Tag} Cleanup failed (not critical): {ex.Message}");
        }
    }

    private static string GetMime(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => throw new NotSupportedException($"{Tag} Unsupported image type. Use png, jpg or webp.")
        };
    }

    private static string Shorten(string text)
    {
        return text.Length > 200 ? text.Substring(0, 200) : text;
    }
}
