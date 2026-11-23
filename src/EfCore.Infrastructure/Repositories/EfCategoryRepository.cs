using EfCore.Application.Common.Interfaces;
using EfCore.Application.Dtos;
using EfCore.Domain.Entities;
using EfCore.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCore.Infrastructure.Repositories;

public sealed class EfCategoryRepository : ICategoryRepository
{
  private readonly AppDbContext _db;
  private readonly ILogger<EfCategoryRepository> _logger;

  public EfCategoryRepository(AppDbContext db, ILogger<EfCategoryRepository> logger)
  {
    _db = db;
    _logger = logger;
  }
  public async Task<CategoryAssignStatus> AssignProductCompareAndSwapAsync(string productCode, int NewCategoryId, int? expectedCategoryId, CancellationToken ct)
  {
     var newCategory = await _db.Categories.AsNoTracking()
            .Where(c => c.Id == NewCategoryId)
            .Select(c => new { c.Name })
            .SingleOrDefaultAsync(ct);
        if (newCategory is null) return CategoryAssignStatus.CategoryNotFound;

        int updated;
        try
        {
            updated = await _db.Products
                .Where(p => p.Code == productCode && p.CategoryId == expectedCategoryId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.CategoryId, NewCategoryId)
                    .SetProperty(p => p.Category.Name, newCategory.Name), ct);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            return CategoryAssignStatus.Conflict;
        }

        if (updated == 1) return CategoryAssignStatus.Updated;

        bool exists = await _db.Products.AsNoTracking()
            .AnyAsync(p => p.Code == productCode, ct);
        return exists ? CategoryAssignStatus.Conflict : CategoryAssignStatus.ProductNotFound;
  }

  public async Task<CategoryCreateResult> CreateWithProductAsync(string name, IReadOnlyList<string> productCodes, CancellationToken ct)
  {
    await using var transaction = await _db.Database.BeginTransactionAsync(ct);
    try
    {
      if(await _db.Categories.AnyAsync(c => c.Name == name, ct))
        return new CategoryCreateResult(CategoryCreateStatus.DuplicateName, Detail: "Name already registered");

      var category = new Category(name);
      _db.Categories.Add(category);
      await _db.SaveChangesAsync(ct);

      var products = await _db.Products
        .Where(p => productCodes.Contains(p.Code))
        .ToListAsync(ct);

      if(products.Count != productCodes.Count)
      {
        await transaction.RollbackAsync(CancellationToken.None);
        return new CategoryCreateResult(CategoryCreateStatus.ProductNotFound, Detail: "ProductCode(s) not found");
      }

      foreach(var product in products) product.AssignCategory(category);

      await _db.SaveChangesAsync(ct);
      await transaction.CommitAsync(ct);

      return new CategoryCreateResult(CategoryCreateStatus.Created, new CategoryCreatedResponse(category.Id, category.Name, productCodes.ToArray()));
    }
    catch (DbUpdateConcurrencyException ex)
    {
      await transaction.RollbackAsync(CancellationToken.None);
      _logger.LogWarning(ex, "Concurrent category assingment detected");
      return new CategoryCreateResult(CategoryCreateStatus.Conflict, Detail: "Product changes by other request");
    }
    catch
    {
      await transaction.RollbackAsync(CancellationToken.None);
      throw;
    }
  }

  public async Task<ProductCategoryStateResponse?> GetProductAssignmentAsync(string productCode, CancellationToken ct)
  {
    var state = await _db.Products.AsNoTracking()
      .Where(p => p.Code == productCode)
      .Select(p => new
      {
        p.Code,
        p.CategoryId,
        p.Category.Name,
        p.RowVersion
      })
      .SingleOrDefaultAsync(ct);

      return state is null ? null : new ProductCategoryStateResponse(state.Code, state.CategoryId, state.Name, Convert.ToBase64String(state.RowVersion));
  }

  public async Task<CategoryProductsResponse?> GetProductsAsync(int categoryId, int page, int pageSize, CancellationToken ct)
  {
    var category = await _db.Categories.AsNoTracking()
      .Where(c => c.Id == categoryId)
      .Select(c => new { c.Id, c.Name })
      .SingleOrDefaultAsync(ct);

    if (category is null) return null;

    var query = _db.Products.AsNoTracking().Where(p => p.CategoryId == categoryId);
    long totalCount = await query.LongCountAsync(ct);

    var items = await query.OrderBy(p => p.Code)
        .Skip((page - 1) * pageSize).Take(pageSize)
        .Select(p => new CategoryProductResponse(
            p.Code, p.Name, p.Price, p.Stock, p.Category!.Name))
        .ToArrayAsync(ct);

    return new CategoryProductsResponse(categoryId, category.Name, page, pageSize, totalCount, items);
  }
}
