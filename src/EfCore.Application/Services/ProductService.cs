using EfCore.Application.Common.Interfaces;
using EfCore.Application.Dtos;
using EfCore.Domain.Entities;
using EfCore.Enums;

namespace EfCore.Application.Services;

public sealed class ProductService : IProductService
{
	private readonly IProductRepository _repository;

	public ProductService(IProductRepository repository)
	{
		_repository = repository;
	}

	public Task<IReadOnlyList<ProductResponse>> GetAllAsync(int page, int size, CancellationToken cancellationToken)
	{
		page = Math.Max(1, page);
		size = Math.Clamp(size, 1, 50);
		if(page > 10_000) throw new ArgumentException("Page terlalu besar");
		return _repository.GetAllAsync(page, size, cancellationToken);
	}
	public async Task<ProductResponse?> GetByCodeAsync(string productCode, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(productCode)
				|| productCode.Trim().Length > 30)
		{
				return null;
		}

		return await _repository.GetByCodeAsync(
				productCode,
				cancellationToken);
	}

	public Task<ProductResponse?> CreateAsync(ProductRequest request, CancellationToken cancellationToken)
	{
		var product = new Product(
				request.Code,
				request.Name,
				request.Price,
				request.Stock,
				request.CategoryId
		);

		return _repository.TryAddAsync(request, cancellationToken);
	}
	public async Task<ProductResponse?> UpdateAsync(string productCode, UpdateProductRequest request, CancellationToken cancellationToken)
	{
		var updated = await _repository.TryUpdateAsync(
				productCode,
				request,
				cancellationToken);

		return updated is null ? null : updated;
	}

	public Task<bool> DeleteAsync(string productCode, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(productCode) || productCode.Trim().Length > 30)
		{
			return Task.FromResult(false);
		}

		return _repository.TryDeleteAsync(
				productCode,
				cancellationToken);
	}

  public Task<bool> ActivateAsync(string code, CancellationToken cancellationToken)
  {
		if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 30)
		{
			return Task.FromResult(false);
		}

		return _repository.TryActivateAsync(code, cancellationToken);
  }

  public Task<bool> DeactivateAsync(string code, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 30)
		{
			return Task.FromResult(false);
		}

		return _repository.TryDeactivateAsync(code, cancellationToken);
  }
  public Task<PageResult<ProductSearchItemResponse>> Search(ProductSearchRequest request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);

		if(request.Page is < 1 or > 1000 || request.PageSize is < 1 or > 50) 	throw new ArgumentOutOfRangeException(nameof(request), "Pagination not valid");

		if(request.MinPrice < 0  || request.MaxPrice < 0 || (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice > request.MaxPrice))
		{
			throw new ArgumentException("Price not valid", nameof(request));
		}

		string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

		string? category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();

		if (search?.Length > 60 || category?.Length > 30)
		{
				throw new ArgumentException("Kepanjangan", nameof(request));
		}

		ProductSort sort = request.Sort?.Trim().ToLowerInvariant() switch
		{
			"code_asc" => ProductSort.CodeAsc,
			"code_desc" => ProductSort.CodeDesc,
			"name_asc" => ProductSort.NameAsc,
			"name_desc" => ProductSort.NameDesc,
			"price_asc" => ProductSort.PriceAsc,
			"price_desc" => ProductSort.PriceDesc,
			"stock_asc" => ProductSort.StockAsc,
			"stock_desc" => ProductSort.StockDesc,

			_ => throw new ArgumentException("Sort tidak dikenali", nameof(request))
		};

		var criteria = new ProductSearchCriteria(
			search, category, request.MinPrice,
			request.MaxPrice, sort,
			request.Page,
			request.PageSize
		);

		return _repository.Search(criteria, cancellationToken);
  }
}
public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(int page, int size, CancellationToken cancellationToken);
    Task<ProductResponse?> GetByCodeAsync(string productCode, CancellationToken cancellationToken);
    Task<ProductResponse?> CreateAsync(ProductRequest request, CancellationToken cancellationToken);
    Task<ProductResponse?> UpdateAsync(string productCode, UpdateProductRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string productCode, CancellationToken cancellationToken);
    Task<bool> ActivateAsync(string code, CancellationToken cancellationToken);
    Task<bool> DeactivateAsync(string code, CancellationToken cancellationToken);
		Task<PageResult<ProductSearchItemResponse>> Search(ProductSearchRequest request, CancellationToken cancellationToken);
}
