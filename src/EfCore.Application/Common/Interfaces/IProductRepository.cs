using EfCore.Application.Dtos;
using EfCore.Enums;

namespace EfCore.Application.Common.Interfaces;

public interface IProductRepository
{
  Task<IReadOnlyList<ProductResponse>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken);
  Task<ProductResponse?> GetByCodeAsync(string code, CancellationToken cancellationToken);
  Task<ProductResponse?> TryAddAsync(ProductRequest request, CancellationToken cancellationToken);
  Task<ProductResponse?> TryUpdateAsync(string code, UpdateProductRequest request, CancellationToken cancellationToken);
  Task<bool> TryDeleteAsync(string code, CancellationToken cancellationToken);
  Task<bool> TryActivateAsync(string code, CancellationToken cancellationToken);
  Task<bool> TryDeactivateAsync(string code, CancellationToken cancellationToken);
  Task<PageResult<ProductSearchItemResponse>> Search(ProductSearchCriteria criteria, CancellationToken ct);
}
