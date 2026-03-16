using BlazorWebAppWithKeycloak.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorWebAppWithKeycloak.API.Data;

/// <summary>
/// EF Core database context voor de todo-functionaliteit.
/// Geconfigureerd voor SQLite; de databaselocatie staat in appsettings.json
/// onder <c>ConnectionStrings:TodoDb</c>.
/// </summary>
public sealed class TodoDbContext(DbContextOptions<TodoDbContext> options)
    : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Username)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.Property(t => t.Titel)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(t => t.Omschrijving)
                  .HasMaxLength(1000);

            entity.Property(t => t.Prioriteit)
                  .HasConversion<string>()   // Opgeslagen als tekst ("Laag", "Normaal", "Hoog")
                  .HasMaxLength(10);

            // Index op Username zodat ophalen van items per gebruiker snel blijft
            entity.HasIndex(t => t.Username);
        });
    }
}
