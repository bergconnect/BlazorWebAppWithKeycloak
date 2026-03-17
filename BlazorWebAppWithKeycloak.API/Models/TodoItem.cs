namespace BlazorWebAppWithKeycloak.API.Models;

/// <summary>
/// Prioriteitsniveau van een todo-item.
/// </summary>
public enum Priority
{
    Laag    = 0,
    Normaal = 1,
    Hoog    = 2
}

/// <summary>
/// Eén todo-item gekoppeld aan een specifieke gebruiker.
/// De gebruiker wordt geïdentificeerd via de <c>preferred_username</c>
/// claim uit het Keycloak JWT-token — consistent met de rest van de applicatie.
/// </summary>
public sealed class TodoItem
{
    public int      Id           { get; set; }

    /// <summary>
    /// Keycloak gebruikersnaam (<c>preferred_username</c>).
    /// Elke gebruiker ziet en beheert alleen zijn eigen items.
    /// </summary>
    public string   Username     { get; set; } = string.Empty;

    public string   Titel        { get; set; } = string.Empty;

    public string?  Omschrijving { get; set; }

    public bool     Afgerond     { get; set; } = false;

    public Priority Prioriteit   { get; set; } = Priority.Normaal;

    public DateOnly? Vervaldatum { get; set; }

    public DateTimeOffset AangemaaktOp { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset GewijzigdOp  { get; set; } = DateTimeOffset.UtcNow;
}
