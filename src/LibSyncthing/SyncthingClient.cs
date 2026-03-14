using System.Net.Http.Json;
using System.Text.Json;

namespace LibSyncthing;

public sealed class SyncthingClient : IDisposable
{
    private readonly HttpClient _http;

    public SyncthingClient(string baseUrl, string apiKey)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(90), // longer than event long-poll timeout
        };
        _http.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    internal async Task<T> GetAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(path, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new SyncthingApiException(
                $"GET {path} returned {(int)response.StatusCode}: {body}",
                (int)response.StatusCode
            );
        }

        return await response.Content.ReadFromJsonAsync<T>(ct).ConfigureAwait(false)
            ?? throw new SyncthingApiException(
                $"GET {path} returned null body",
                (int)response.StatusCode
            );
    }

    internal async Task PostAsync(string path, CancellationToken ct = default)
    {
        var response = await _http.PostAsync(path, null, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new SyncthingApiException(
                $"POST {path} returned {(int)response.StatusCode}: {body}",
                (int)response.StatusCode
            );
        }
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
