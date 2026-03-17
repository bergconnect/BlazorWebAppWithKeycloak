using BlazorWebAppWithKeycloak.API.Models;
using BlazorWebAppWithKeycloak.API.Repositories;

namespace BlazorWebAppWithKeycloak.API.Services;

/// <summary>
/// Implementatie van <see cref="ITodoService"/>.
/// Bevat alle businesslogica, validatie en mapping tussen DTOs en entiteiten.
/// Delegeert databewerkingen naar <see cref="ITodoRepository"/>.
/// </summary>
public sealed class TodoService(ITodoRepository repository) : ITodoService
{
    public async Task<IReadOnlyList<TodoResponse>> GetAlleAsync(
        string username, CancellationToken ct = default)
    {
        var items = await repository.GetAlleAsync(username, ct);
        return items.Select(TodoResponse.FromEntity).ToList();
    }

    public async Task<TodoResponse?> GetAsync(
        int id, string username, CancellationToken ct = default)
    {
        var item = await repository.GetAsync(id, username, ct);
        return item is null ? null : TodoResponse.FromEntity(item);
    }

    public async Task<TodoResponse> AanmakenAsync(
        TodoAanmakenRequest request, string username, CancellationToken ct = default)
    {
        // ── Validatie ─────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(request.Titel))
            throw new ArgumentException("Titel mag niet leeg zijn.", nameof(request));

        if (request.Vervaldatum.HasValue && request.Vervaldatum.Value < DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Vervaldatum mag niet in het verleden liggen.", nameof(request));

        // ── Mapping: request → entiteit ───────────────────────────────────────
        var item = new TodoItem
        {
            Username     = username,
            Titel        = request.Titel.Trim(),
            Omschrijving = request.Omschrijving?.Trim(),
            Prioriteit   = request.Prioriteit,
            Vervaldatum  = request.Vervaldatum,
        };

        var aangemaakt = await repository.AanmakenAsync(item, ct);

        // ── Mapping: entiteit → response ──────────────────────────────────────
        return TodoResponse.FromEntity(aangemaakt);
    }

    public async Task<TodoResponse?> BijwerkenAsync(
        int id, TodoBijwerkenRequest request, string username, CancellationToken ct = default)
    {
        var item = await repository.GetAsync(id, username, ct);
        if (item is null) return null;

        // ── Validatie ─────────────────────────────────────────────────────────
        if (request.Titel is not null && string.IsNullOrWhiteSpace(request.Titel))
            throw new ArgumentException("Titel mag niet leeg zijn.", nameof(request));

        if (request.Vervaldatum.HasValue && request.Vervaldatum.Value < DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Vervaldatum mag niet in het verleden liggen.", nameof(request));

        // ── Mapping: request → entiteit (alleen gevulde velden) ───────────────
        if (request.Titel        is not null) item.Titel        = request.Titel.Trim();
        if (request.Omschrijving is not null) item.Omschrijving = request.Omschrijving.Trim();
        if (request.Afgerond     is not null) item.Afgerond     = request.Afgerond.Value;
        if (request.Prioriteit   is not null) item.Prioriteit   = request.Prioriteit.Value;
        if (request.Vervaldatum  is not null) item.Vervaldatum  = request.Vervaldatum;

        var bijgewerkt = await repository.OpslaanAsync(item, ct);

        // ── Mapping: entiteit → response ──────────────────────────────────────
        return TodoResponse.FromEntity(bijgewerkt);
    }

    public async Task<TodoResponse?> ToggleAfgerondAsync(
        int id, string username, CancellationToken ct = default)
    {
        var item = await repository.GetAsync(id, username, ct);
        if (item is null) return null;

        item.Afgerond = !item.Afgerond;

        var bijgewerkt = await repository.OpslaanAsync(item, ct);
        return TodoResponse.FromEntity(bijgewerkt);
    }

    public async Task<bool> VerwijderenAsync(
        int id, string username, CancellationToken ct = default)
    {
        var item = await repository.GetAsync(id, username, ct);
        if (item is null) return false;

        await repository.VerwijderenAsync(item, ct);
        return true;
    }
}
