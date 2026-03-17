namespace BlazorWebAppWithKeycloak.Services;

// ── Gedeelde enumeratie ────────────────────────────────────────────────────────

public enum Priority
{
    Laag = 0,
    Normaal = 1,
    Hoog = 2
}

// ── Response DTO (spiegelt de API) ─────────────────────────────────────────────

public sealed record TodoResponse(
    int Id,
    string Titel,
    string? Omschrijving,
    bool Afgerond,
    Priority Prioriteit,
    DateOnly? Vervaldatum,
    DateTimeOffset AangemaaktOp,
    DateTimeOffset GewijzigdOp
);

// ── Request records ────────────────────────────────────────────────────────────

public sealed record TodoAanmakenRequest(
    string Titel,
    string? Omschrijving,
    Priority Prioriteit,
    DateOnly? Vervaldatum
);

public sealed record TodoBijwerkenRequest(
    string? Titel,
    string? Omschrijving,
    bool? Afgerond,
    Priority? Prioriteit,
    DateOnly? Vervaldatum
);

// ── Typed client ───────────────────────────────────────────────────────────────

/// <summary>
/// Typed HttpClient voor alle todo-endpoints van de API.
/// Het Bearer token wordt automatisch toegevoegd via <see cref="BearerTokenHandler"/>.
/// </summary>
public sealed class TodoApiClient(HttpClient httpClient)
{
    private const string Base = "/api/todos";

    public Task<List<TodoResponse>?> GetAlleAsync(CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<List<TodoResponse>>(Base, ct);

    public Task<TodoResponse?> GetAsync(int id, CancellationToken ct = default)
        => httpClient.GetFromJsonAsync<TodoResponse>($"{Base}/{id}", ct);

    public async Task<TodoResponse?> AanmakenAsync(
        TodoAanmakenRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(Base, request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TodoResponse>(ct);
    }

    public async Task<TodoResponse?> BijwerkenAsync(
        int id, TodoBijwerkenRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"{Base}/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TodoResponse>(ct);
    }

    public async Task<TodoResponse?> ToggleAfgerondAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsync($"{Base}/{id}/afgerond", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TodoResponse>(ct);
    }

    public async Task VerwijderenAsync(int id, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"{Base}/{id}", ct);
        response.EnsureSuccessStatusCode();
    }
}
