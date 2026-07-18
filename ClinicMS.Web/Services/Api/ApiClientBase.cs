using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClinicMS.Web.Services.Api;

public abstract class ApiClientBase
{
    protected readonly HttpClient Http;

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    protected ApiClientBase(HttpClient http)
    {
        Http = http;
    }

    protected async Task<T> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await Http.GetAsync(requestUri, cancellationToken);
        return await ReadOrThrowAsync<T>(response, cancellationToken);
    }

    /// <summary>For singleton-row "not configured yet" endpoints that return 200 with a null body
    /// instead of 404.</summary>
    protected async Task<T?> GetOrDefaultAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await Http.GetAsync(requestUri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    /// <summary>For endpoints that return 404 (not 200-with-null) when nothing's configured yet --
    /// treats a 404 as "no data" rather than an error.</summary>
    protected async Task<T?> GetOptionalAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await Http.GetAsync(requestUri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        return await ReadOrThrowAsync<T>(response, cancellationToken);
    }

    protected async Task<T> PostAsync<T>(string requestUri, object? body, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PostAsJsonAsync(requestUri, body, JsonOptions, cancellationToken);
        return await ReadOrThrowAsync<T>(response, cancellationToken);
    }

    protected async Task PostAsync(string requestUri, object? body, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PostAsJsonAsync(requestUri, body, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    protected async Task<T> PutAsync<T>(string requestUri, object? body, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PutAsJsonAsync(requestUri, body, JsonOptions, cancellationToken);
        return await ReadOrThrowAsync<T>(response, cancellationToken);
    }

    protected async Task PutAsync(string requestUri, object? body, CancellationToken cancellationToken = default)
    {
        using var response = await Http.PutAsJsonAsync(requestUri, body, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    protected async Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await Http.DeleteAsync(requestUri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task<T> ReadOrThrowAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return result ?? throw new ApiException((int)response.StatusCode, "The server returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string message = $"Request failed with status {(int)response.StatusCode}.";
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiErrorPayload>(JsonOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(problem?.Title))
            {
                message = problem.Title;
            }
        }
        catch (JsonException)
        {
            // Body wasn't the expected { status, title, errors } shape -- fall back to the generic message.
        }

        throw new ApiException((int)response.StatusCode, message);
    }

    private sealed record ApiErrorPayload(int Status, string? Title, object? Errors);
}
