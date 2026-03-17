using BlazorWebAppWithKeycloak.API.Data;
using BlazorWebAppWithKeycloak.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorWebAppWithKeycloak.API.Repositories;

/// <summary>
/// EF Core implementatie van <see cref="ITodoRepository"/>.
/// Alle queries zijn gefilterd op username zodat gebruikers
/// nooit elkaars items kunnen lezen of wijzigen.
/// </summary>
public sealed class TodoRepository(TodoDbContext db) : ITodoRepository
{
    public async Task<IReadOnlyList<TodoItem>> GetAlleAsync(
        string username, CancellationToken ct = default)
    {
        return await db.TodoItems
            .Where(t => t.Username == username)
            .OrderBy(t => t.Afgerond)
            .ThenBy(t => t.Vervaldatum)
            .ThenByDescending(t => t.Prioriteit)
            .ToListAsync(ct);
    }

    public Task<TodoItem?> GetAsync(
        int id, string username, CancellationToken ct = default)
    {
        return db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.Username == username, ct);
    }

    public async Task<TodoItem> AanmakenAsync(
        TodoItem item, CancellationToken ct = default)
    {
        db.TodoItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<TodoItem> OpslaanAsync(
        TodoItem item, CancellationToken ct = default)
    {
        item.GewijzigdOp = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task VerwijderenAsync(
        TodoItem item, CancellationToken ct = default)
    {
        db.TodoItems.Remove(item);
        await db.SaveChangesAsync(ct);
    }
}
