namespace BlazorWebAppWithKeycloak.Services;

/// <summary>
/// Response model voor de /api/hello en /api/admin endpoints.
/// </summary>
public sealed record HelloWorldResponse(string Message, DateTimeOffset Timestamp);

/// <summary>
/// Typed HttpClient voor de BlazorWebAppWithKeycloak.API.
/// Voegt automatisch het access token toe aan elke aanroep via <see cref="BearerTokenHandler"/>.
/// </summary>
public sealed class HelloWorldApiClient(HttpClient httpClient)
{
    private const string HelloEndpoint = "/api/hello";
    private const string AdminEndpoint = "/api/admin";

    /// <summary>
    /// Roept GET /api/hello aan — vereist de 'user' rol.
    /// </summary>
    public async Task<HelloWorldResponse> GetHelloAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<HelloWorldResponse>(
            HelloEndpoint, cancellationToken);

        return response ?? throw new InvalidOperationException(
            "API retourneerde een lege response.");
    }

    /// <summary>
    /// Roept GET /api/admin aan — vereist de 'admin' rol.
    /// </summary>
    public async Task<HelloWorldResponse> GetAdminAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<HelloWorldResponse>(
            AdminEndpoint, cancellationToken);

        return response ?? throw new InvalidOperationException(
            "API retourneerde een lege response.");
    }
}
