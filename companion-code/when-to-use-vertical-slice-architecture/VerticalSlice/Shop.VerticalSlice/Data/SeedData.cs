using Microsoft.EntityFrameworkCore;

namespace Shop.VerticalSlice.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext context)
    {
        if (await context.DiscountCodes.AnyAsync())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        context.DiscountCodes.AddRange(
            new DiscountCodeEntity { Id = Guid.NewGuid(), Code = "SAVE10", Type = DiscountType.Percentage, Value = 10, MinimumOrderAmount = 0, IsStackable = true, MaxRedemptions = 1000, ExpiresAtUtc = now.AddYears(1) },
            new DiscountCodeEntity { Id = Guid.NewGuid(), Code = "FLAT20", Type = DiscountType.FixedAmount, Value = 20, MinimumOrderAmount = 50, IsStackable = false, MaxRedemptions = 1000, ExpiresAtUtc = now.AddYears(1) },
            new DiscountCodeEntity { Id = Guid.NewGuid(), Code = "BIGSPENDER", Type = DiscountType.Percentage, Value = 15, MinimumOrderAmount = 500, IsStackable = true, MaxRedemptions = 1000, ExpiresAtUtc = now.AddYears(1) });

        await context.SaveChangesAsync();
    }
}
