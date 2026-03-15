namespace BlazorWebAppWithKeycloak.Services;

/// <summary>
/// Response model voor het /api/hello endpoint.
/// </summary>
public sealed record HelloWorldResponse(string Message, DateTimeOffset Timestamp);

/// <summary>
/// Typed HttpClient voor de BlazorWebAppWithKeycloak.API.
/// Voegt automatisch het access token toe aan elke aanroep via <see cref="BearerTokenHandler"/>.
/// </summary>
public sealed class HelloWorldApiClient(HttpClient httpClient)
{
    private const string HelloEndpoint = "/api/hello";

    /// <summary>
    /// Roept GET /api/hello aan en retourneert het antwoord.
    /// Gooit een <see cref="HttpRequestException"/> bij een niet-succesvolle statuscode.
    /// </summary>
    public async Task<HelloWorldResponse> GetHelloAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<HelloWorldResponse>(
            HelloEndpoint, cancellationToken);

        return response ?? throw new InvalidOperationException(
            "API retourneerde een lege response.");
    }
}
