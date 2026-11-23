using System.ComponentModel.DataAnnotations;
using EfCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EfCore.Application.Dtos;

public record ProductRequest(
    [Required, StringLength(30)]
    string Code,

    [Required]
    [StringLength(120)]
    string Name,

    [Required]
    [Range(typeof(decimal), "0", "99999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    decimal Price,

    [Required]
    [Range(0, int.MaxValue)]
    int Stock,

    [Required, Range(0, 99999)]
    int CategoryId,

    [Required]
    bool IsActive,

    [DataType(DataType.Date)]
    DateTime DeactivatedAtUtc
);

public record UpdateProductRequest(
    [Required]
    [StringLength(120)]
    string Name,

    [Required]
    [Range(typeof(decimal), "0", "99999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    decimal Price,

    [Required]
    [Range(0, int.MaxValue)]
    int Stock,

    [Required, Range(0, 99999)]
    int CategoryId,

    [Required]
    bool IsActive,

    [DataType(DataType.Date)]
    DateTime DeactivatedAtUtc
);

public sealed record ProductResponse(string Code, string Name, decimal Price, int Stock, CategoryResponse? Category);


public sealed class ProductSearchRequest : IValidatableObject
{
  [FromQuery(Name = "search")]
  [StringLength(100, ErrorMessage = "Too Long")]
  public string? Search {get; set;}

  [FromQuery(Name = "category")]
  public string? Category {get; set;}
  [FromQuery(Name = "min_price")]
  public decimal? MinPrice {get; set;}
  [FromQuery(Name = "max_price")]
  public decimal? MaxPrice {get; set;}
  [FromQuery(Name = "sort")]
  [Required, StringLength(30)]
  public string Sort {get; set;} = "code_asc";
  [FromQuery(Name = "page")]
  [Range(1, 1000)]
  public int Page {get; set;} = 1;
  [FromQuery(Name = "page_size")]
  [Range(1, 100)]
  public int PageSize {get; set;} = 10;

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if(MinPrice < 0)
    {
      yield return new ValidationResult("min_price cannot be lower than 0", [nameof(MinPrice)]);
    }

    if(MaxPrice < 0)
    {
      yield return new ValidationResult("max_price cannot be lower than 0", [nameof(MaxPrice)]);
    }

    if(MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
    {
      yield return new ValidationResult("min_price should not be greater max_price", [nameof(MinPrice), nameof(MaxPrice)]);
    }

    string[] allowedSort = [
      "code_asc", "code_desc", "name_asc", "name_desc", "price_asc", "price_desc", "stock_asc", "stock_desc"
    ];

    if(!allowedSort.Contains(Sort, StringComparer.OrdinalIgnoreCase))
    {
      yield return new ValidationResult("Sort not found", [nameof(Sort)]);
    }
  }
}

public sealed record ProductSearchItemResponse(string Code, string Name, CategoryResponse? Category, decimal Price, int Stock);

public sealed record PageResult<T>(int Page, int PageSize, long TotalCount, long TotalPages, IReadOnlyList<T> Items);
