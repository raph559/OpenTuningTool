using System.Net.Http.Headers;
using System.Text.Json;

namespace OpenTuningTool.Services;

/// <summary>
/// Thin HTTP client wrapper for the CalibrAI REST API (localhost:8721).
/// </summary>
public class CalibrAiClient : IDisposable
{
    private readonly HttpClient _http = new();
    private string _baseUrl;

    public CalibrAiClient(string? baseUrl = null)
    {
        _baseUrl = NormalizeBaseUrl(baseUrl);
    }

    public void SetBaseUrl(string? baseUrl)
    {
        _baseUrl = NormalizeBaseUrl(baseUrl);
    }

    /// <summary>
    /// POST a BIN file to CalibrAI and return detected map candidates.
    /// </summary>
    /// <exception cref="HttpRequestException">Thrown when the server is unreachable.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the server returns 503 (no model loaded).</exception>
    public async Task<List<MapCandidateResult>> DetectAsync(
        string binPath, float minConfidence = 0.3f)
    {
        using var content = new MultipartFormDataContent();

        byte[] fileBytes = await File.ReadAllBytesAsync(binPath);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(binPath));

        HttpResponseMessage resp = await _http.PostAsync(
            $"{_baseUrl}/detect?min_confidence={minConfidence}", content);

        if (resp.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            throw new InvalidOperationException(
                "CalibrAI has no trained model loaded.\n\n" +
                "Build a dataset and train a model first:\n" +
                "  calibrai build-dataset\n" +
                "  calibrai train");

        resp.EnsureSuccessStatusCode();

        string json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<MapCandidateResult>>(json)
               ?? new List<MapCandidateResult>();
    }

    /// <summary>
    /// Check whether the CalibrAI server is reachable and whether a model is loaded.
    /// Returns null on success, otherwise a user-friendly error string.
    /// </summary>
    public async Task<string?> HealthCheckAsync()
    {
        try
        {
            HttpResponseMessage resp = await _http.GetAsync($"{_baseUrl}/health");
            return resp.IsSuccessStatusCode ? null : $"Server returned {(int)resp.StatusCode}";
        }
        catch (HttpRequestException)
        {
            return "CalibrAI server is not running.";
        }
    }

    private static string NormalizeBaseUrl(string? baseUrl)
    {
        string normalized = (baseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return "http://localhost:8721";

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"http://{normalized}";
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
            return "http://localhost:8721";

        return uri.ToString().TrimEnd('/');
    }

    public void Dispose() => _http.Dispose();
}
