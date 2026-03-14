namespace BlazorWebAppWithKeycloak.Services;

/// <summary>
/// Response model voor het /api/hello endpoint.
/// </summary>
public sealed record HelloWorldResponse(string Message, DateTimeOffset Timestamp);

/// <summary>
/// Typed HttpClient voor de BlazorWebAppWithKeycloak.API.
/// Voegt automatisch het access token toe aan elke aanroep.
/// </summary>
public sealed class HelloWorldApiClient(HttpClient httpClient)
{
    /// <summary>
    /// Roept GET /api/hello aan en retourneert het antwoord.
    /// Gooit een <see cref="HttpRequestException"/> bij een niet-succesvolle statuscode.
    /// </summary>
    public async Task<HelloWorldResponse> GetHelloAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<HelloWorldResponse>(
            "/api/hello", cancellationToken);

        return response ?? throw new InvalidOperationException(
            "API retourneerde een lege response.");
    }
}
