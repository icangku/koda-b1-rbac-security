using EfCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Infrastructure.Seeding;

public static class ProductSeedData
{
  private static IEnumerable<Product> InitialProducts()
  {
    for(int i = 1; i <= 500; i++)
    {
      string code = $"P{i:D3}";
      string name = $"Product Dummy {i}";

      decimal price = 10000m + (i * 1000m);
      int stock = 20;

      int category = i % 3 == 0 ? 1 : (i % 2 == 0 ? 2 : 3);
      bool isActive = i % 2 == 0 ? false : true;

      yield return new Product(code, name, price, stock, category);
    }
  }

  public static void Seed(DbContext context)
  {
    foreach(var product in InitialProducts())
    {
      bool exists = context.Set<Product>().Any(p => p.Code == product.Code);

      if (!exists)
      {
        context.Set<Product>().Add(product);
      }
    }
    context.SaveChanges();
  }
  public static async Task SeedAsync(DbContext context, CancellationToken ct)
  {
    foreach (var product in InitialProducts())
    {
      bool exists = await context.Set<Product>().AnyAsync(p => p.Code == product.Code, ct);

      if (!exists)
      {
        context.Set<Product>().Add(product);
      }
    }
    await context.SaveChangesAsync(ct);
  }
}
