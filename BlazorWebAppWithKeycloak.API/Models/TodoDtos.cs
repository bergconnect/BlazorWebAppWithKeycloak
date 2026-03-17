using System.ComponentModel.DataAnnotations;

namespace BlazorWebAppWithKeycloak.API.Models;

/// <summary>
/// Request body voor het aanmaken van een nieuw todo-item.
/// </summary>
public sealed record TodoAanmakenRequest(
    [Required, MaxLength(200)] string  Titel,
    [MaxLength(1000)]          string? Omschrijving,
                               Priority Prioriteit  = Priority.Normaal,
                               DateOnly? Vervaldatum = null
);

/// <summary>
/// Request body voor het bijwerken van een bestaand todo-item.
/// Alleen meegegeven velden worden overschreven.
/// </summary>
public sealed record TodoBijwerkenRequest(
    [MaxLength(200)]  string?  Titel,
    [MaxLength(1000)] string?  Omschrijving,
                      bool?    Afgerond,
                      Priority? Prioriteit,
                      DateOnly? Vervaldatum
);

/// <summary>
/// Response DTO — verstuurd naar de client.
/// Bevat geen interne velden zoals Username.
/// </summary>
public sealed record TodoResponse(
    int              Id,
    string           Titel,
    string?          Omschrijving,
    bool             Afgerond,
    Priority         Prioriteit,
    DateOnly?        Vervaldatum,
    DateTimeOffset   AangemaaktOp,
    DateTimeOffset   GewijzigdOp
)
{
    /// <summary>Converteert een <see cref="TodoItem"/> entiteit naar een response DTO.</summary>
    public static TodoResponse FromEntity(TodoItem item) => new(
        item.Id,
        item.Titel,
        item.Omschrijving,
        item.Afgerond,
        item.Prioriteit,
        item.Vervaldatum,
        item.AangemaaktOp,
        item.GewijzigdOp
    );
}
