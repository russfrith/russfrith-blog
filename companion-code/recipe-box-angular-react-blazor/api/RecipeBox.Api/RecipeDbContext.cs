using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RecipeBox.Shared;

namespace RecipeBox.Api;

public class RecipeDbContext(DbContextOptions<RecipeDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var utc = new ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.Property(r => r.CreatedAt).HasConversion(utc);
            entity.Property(r => r.UpdatedAt).HasConversion(utc);

            // Ingredients/Steps have no stable identity and PUT replaces them
            // wholesale, so a JSON column (whole-document replace) fits better
            // than a child table. See the Phase 1 plan's "Persistence" note.
            entity.OwnsMany(r => r.Ingredients, i => i.ToJson());
            entity.OwnsMany(r => r.Steps, s => s.ToJson());
        });
    }
}
