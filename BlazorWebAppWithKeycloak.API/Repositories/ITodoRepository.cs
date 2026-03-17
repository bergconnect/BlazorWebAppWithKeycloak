using BlazorWebAppWithKeycloak.API.Models;

namespace BlazorWebAppWithKeycloak.API.Repositories;

/// <summary>
/// Definieert de databewerkingen voor todo-items van een specifieke gebruiker.
/// Alle methoden zijn beperkt tot de items van de opgegeven gebruikersnaam.
/// </summary>
public interface ITodoRepository
{
    /// <summary>Geeft alle todo-items terug van de opgegeven gebruiker, gesorteerd op status, vervaldatum en prioriteit.</summary>
    Task<IReadOnlyList<TodoItem>> GetAlleAsync(string username, CancellationToken ct = default);

    /// <summary>Geeft één todo-item terug op basis van id én gebruikersnaam, of null als het niet bestaat.</summary>
    Task<TodoItem?> GetAsync(int id, string username, CancellationToken ct = default);

    /// <summary>Voegt een nieuw todo-item toe en geeft het opgeslagen item terug.</summary>
    Task<TodoItem> AanmakenAsync(TodoItem item, CancellationToken ct = default);

    /// <summary>Slaat wijzigingen op aan een bestaand item en geeft het bijgewerkte item terug.</summary>
    Task<TodoItem> OpslaanAsync(TodoItem item, CancellationToken ct = default);

    /// <summary>Verwijdert een todo-item. Gooit geen fout als het item niet bestaat.</summary>
    Task VerwijderenAsync(TodoItem item, CancellationToken ct = default);
}
