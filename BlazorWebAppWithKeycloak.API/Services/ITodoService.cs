using BlazorWebAppWithKeycloak.API.Models;

namespace BlazorWebAppWithKeycloak.API.Services;

/// <summary>
/// Definieert de businesslogica voor todo-items.
/// Verantwoordelijk voor validatie, regels en mapping tussen DTOs en entiteiten.
/// </summary>
public interface ITodoService
{
    /// <summary>Geeft alle todo-items terug van de opgegeven gebruiker als response DTOs.</summary>
    Task<IReadOnlyList<TodoResponse>> GetAlleAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// Geeft één todo-item terug, of null als het niet bestaat of niet van de gebruiker is.
    /// </summary>
    Task<TodoResponse?> GetAsync(int id, string username, CancellationToken ct = default);

    /// <summary>
    /// Maakt een nieuw todo-item aan.
    /// Gooit een <see cref="ArgumentException"/> als de titel leeg is.
    /// </summary>
    Task<TodoResponse> AanmakenAsync(TodoAanmakenRequest request, string username, CancellationToken ct = default);

    /// <summary>
    /// Werkt een bestaand todo-item bij.
    /// Geeft null terug als het item niet bestaat of niet van de gebruiker is.
    /// Gooit een <see cref="ArgumentException"/> als de nieuwe titel leeg is.
    /// </summary>
    Task<TodoResponse?> BijwerkenAsync(int id, TodoBijwerkenRequest request, string username, CancellationToken ct = default);

    /// <summary>
    /// Wisselt de afgerond-status van een todo-item.
    /// Geeft null terug als het item niet bestaat of niet van de gebruiker is.
    /// </summary>
    Task<TodoResponse?> ToggleAfgerondAsync(int id, string username, CancellationToken ct = default);

    /// <summary>
    /// Verwijdert een todo-item.
    /// Geeft false terug als het item niet bestaat of niet van de gebruiker is.
    /// </summary>
    Task<bool> VerwijderenAsync(int id, string username, CancellationToken ct = default);
}
