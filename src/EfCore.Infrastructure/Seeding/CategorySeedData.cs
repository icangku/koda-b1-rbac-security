using EfCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KodaStore.Infrastructure.Seeding;

public static class CategorySeedData
{
    public static void Seed(DbContext context)
    {
        var products = context.Set<Product>().ToList();
        var categories = context.Set<Category>().ToList();
        var names = products.Select(p => p.Category.Name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
        {
            if (categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
                continue;
            var category = new Category(name);
            context.Set<Category>().Add(category);
            categories.Add(category);
        }
        context.SaveChanges(); // Memastikan CategoryId tersedia dari IDENTITY.

        // Hubungkan produk lama ke kategori; tidak ubah yang sudah sesuai.
        foreach (var product in products)
        {
            var category = categories.Single(c => string.Equals(
                c.Name, product.Category.Name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (product.CategoryId != category.Id)
                product.AssignCategory(category);
        }
        context.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, CancellationToken ct)
    {
        var products = await context.Set<Product>().ToListAsync(ct);
        var categories = await context.Set<Category>().ToListAsync(ct);
        var names = products.Select(p => p.Category.Name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
        {
            if (categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
                continue;
            var category = new Category(name);
            context.Set<Category>().Add(category);
            categories.Add(category);
        }
        await context.SaveChangesAsync(ct);

        foreach (var product in products)
        {
            var category = categories.Single(c => string.Equals(
                c.Name, product.Category.Name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (product.CategoryId != category.Id)
                product.AssignCategory(category);
        }
        await context.SaveChangesAsync(ct);
    }
}