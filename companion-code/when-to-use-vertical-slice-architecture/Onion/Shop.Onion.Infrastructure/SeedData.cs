using Microsoft.EntityFrameworkCore;
using Shop.Onion.Domain;

namespace Shop.Onion.Infrastructure;

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
            DiscountCode.Create("SAVE10", DiscountType.Percentage, 10, minimumOrderAmount: 0, isStackable: true, maxRedemptions: 1000, expiresAtUtc: now.AddYears(1)),
            DiscountCode.Create("FLAT20", DiscountType.FixedAmount, 20, minimumOrderAmount: 50, isStackable: false, maxRedemptions: 1000, expiresAtUtc: now.AddYears(1)),
            DiscountCode.Create("BIGSPENDER", DiscountType.Percentage, 15, minimumOrderAmount: 500, isStackable: true, maxRedemptions: 1000, expiresAtUtc: now.AddYears(1)));

        await context.SaveChangesAsync();
    }
}
