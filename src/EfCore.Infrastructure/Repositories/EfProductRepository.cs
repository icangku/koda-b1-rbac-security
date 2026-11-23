using EfCore.Application.Common.Interfaces;
using EfCore.Application.Dtos;
using EfCore.Domain.Entities;
using EfCore.Enums;
using EfCore.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Infrastructure.Repositories;

public sealed class EfProductRepository : IProductRepository
{
  private readonly AppDbContext _db;
  public EfProductRepository(AppDbContext appDbContext)
  {
    _db = appDbContext;
  }
  public async Task<PageResult<ProductSearchItemResponse>> Search(ProductSearchCriteria criteria, CancellationToken ct)
  {
    IQueryable<Product> query = _db.Products.AsNoTracking();

    if(criteria.Search is {Length : > 0} keyword) query = query.Where(p => p.Code.Contains(keyword) || p.Name.Contains(keyword));

    if(criteria.Category is { Length : > 0 } category)  query = query.Where(p => p.Category.Name == category);
    
    if(criteria.MinPrice is decimal minPrice) query = query.Where(p => p.Price >= minPrice);

    if (criteria.MaxPrice is decimal maxPrice) query = query.Where(p => p.Price <= maxPrice);

    long TotalCount = await query.LongCountAsync(ct);

    IOrderedQueryable<Product> sorted = criteria.Sort switch
    {
      ProductSort.CodeAsc => query.OrderBy(p => p.Code),
      ProductSort.CodeDesc => query.OrderByDescending(p => p.Code),
      ProductSort.NameAsc => query.OrderBy(p => p.Name),
      ProductSort.NameDesc => query.OrderByDescending(p => p.Name),
      ProductSort.PriceAsc => query.OrderBy(p => p.Price),
      ProductSort.PriceDesc => query.OrderByDescending(p => p.Price),
      ProductSort.StockAsc => query.OrderBy(p => p.Stock),
      ProductSort.StockDesc => query.OrderByDescending(p => p.Stock),
      _ => throw new ArgumentOutOfRangeException(nameof(criteria.Sort))
    };

    int skip  = (criteria.Page - 1) * criteria.PageSize;

    var inspectQuery = sorted.TagWith("EfCore.ProductSearch")
    .Skip(skip)
    .Take(criteria.PageSize)
    .Select(p => new ProductSearchItemResponse(
      p.Code,
      p.Name,
      p.Category == null ? null : new CategoryResponse(p.Category.Id, p.Category.Name),
      p.Price,
      p.Stock
    ));

    string sqlPreview = inspectQuery.ToQueryString();
    System.Diagnostics.Debug.WriteLine(sqlPreview);

    ProductSearchItemResponse[] items  = await inspectQuery.ToArrayAsync(ct);

    long TotalPages = (TotalCount / criteria.PageSize) + (TotalCount % criteria.PageSize == 0 ? 0 : 1);

    return new PageResult<ProductSearchItemResponse>(
      criteria.Page, criteria.PageSize, TotalCount, TotalPages, items
    );
  }

  private static string Normalize(string code) => code.Trim().ToUpperInvariant();
  private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken ct)
  {
    if (!await _db.Categories.AnyAsync(c => c.Id == categoryId, ct))
      throw new ArgumentException($"Category {categoryId} not found.", nameof(categoryId));
  }

  public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
  {
    var query = _db.Products
      .AsNoTracking()
      .OrderBy(p => p.Code)
      .Skip((page -1) * pageSize)
      .Take(pageSize)
      .Select(p => new ProductResponse(
        p.Code,
        p.Name,
        p.Price,
        p.Stock,
        p.Category == null ? null : new CategoryResponse(p.Category.Id, p.Category.Name)
      ));
    
    return await query.ToListAsync(cancellationToken);
  }

  public async Task<ProductResponse?> GetByCodeAsync(string code, CancellationToken cancellationToken)
  {
    var codNor = Normalize(code);
    var product = await _db.Products
      .AsNoTracking()
      .Include(c => c.Category)
      .FirstOrDefaultAsync(p => p.Code == codNor, cancellationToken);

    return product is null 
      ? null 
      : new ProductResponse(
          product.Code, 
          product.Name, 
          product.Price, 
          product.Stock, 
          product.Category == null ? null : new CategoryResponse(product.Category.Id, product.Category.Name)
        );
  }

  public async Task<bool> TryActivateAsync(string code, CancellationToken cancellationToken)
  {
    var exists = await _db.Products.FirstOrDefaultAsync(p => p.Code == code.Trim().ToUpperInvariant(), cancellationToken);
    if (exists is null)
    {
      return false;
    }
    exists.Activate();

    await _db.SaveChangesAsync(cancellationToken);
    return true;
  }

  public async Task<ProductResponse?> TryAddAsync(ProductRequest request, CancellationToken cancellationToken)
  {
    var normalizedCode = Normalize(request.Code);
    if (await _db.Products.AnyAsync(p => p.Code == normalizedCode, cancellationToken))
    {
      return null;
    }

    await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

    _db.Products.Add(new Product(request.Code, request.Name, request.Price, request.Stock, request.CategoryId));
    try
    {
      await _db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
    {
      return null;
    }

    return await GetByCodeAsync(request.Code, cancellationToken);
  }

  public async Task<bool> TryDeactivateAsync(string code, CancellationToken cancellationToken)
  {
    var exists = await _db.Products.FirstOrDefaultAsync(p => p.Code == code.Trim().ToUpperInvariant());

    if(exists is null){
      return false;
    }

    exists.Deactivate();
    await _db.SaveChangesAsync(cancellationToken);

    return true;
  }

  public async Task<bool> TryDeleteAsync(string code, CancellationToken cancellationToken)
  {
    var existing = await _db.Products.FirstOrDefaultAsync(p => p.Code == code.Trim().ToUpperInvariant(), cancellationToken);
    if(existing is null)
    {
      return false;
    }

    _db.Products.Remove(existing);
    await _db.SaveChangesAsync(cancellationToken);

    return true;
  }

  public async Task<ProductResponse?> TryUpdateAsync(string code, UpdateProductRequest request, CancellationToken cancellationToken)
  {
    var existing = await _db.Products.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    if(existing is null) return null;
    
    await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);

    existing.UpdateDetail(
      request.Name,
      request.Price,
      request.Stock,
      request.CategoryId
    );

    await _db.SaveChangesAsync(cancellationToken);
    await _db.Entry(existing).Reference(p => p.Category).LoadAsync(cancellationToken);
    return new ProductResponse(
      existing.Code, 
      existing.Name, 
      existing.Price, 
      existing.Stock, 
      existing.Category == null ? null : new CategoryResponse(existing.Category.Id, existing.Category.Name)
    );
  }
}
