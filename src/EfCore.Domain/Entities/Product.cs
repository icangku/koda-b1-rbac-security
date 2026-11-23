namespace EfCore.Domain.Entities;

public class Product
{
  private Product() { }

  public Product(string code, string name, decimal price, int stock, int categoryId)
  {
    Code = code.Trim().ToUpperInvariant();
    ApplyDetail(name, price, stock, categoryId);
  }

  public string Code { get; private set; } = null!;
  public string Name { get; private set; } = null!;
  public decimal Price { get; private set; }
  public int Stock { get; private set; }

  public int CategoryId { get; private set; }
  public Category Category { get; private set; } = null!;

  public bool IsActive { get; private set; } = true;
  public DateTime? DeactivatedAtUtc { get; private set; }
  public byte[] RowVersion {get; private set;} = [];

  public ICollection<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();

  public void UpdateDetail(string name, decimal price, int stock, int categoryId)
      => ApplyDetail(name, price, stock, categoryId);

  public void Activate()
  {
    IsActive = true;
    DeactivatedAtUtc = null;
  }

  public void Deactivate()
  {
    if (!IsActive) return; 
    IsActive = false;
    DeactivatedAtUtc = DateTime.UtcNow;
  }

  private void ApplyDetail(string name, decimal price, int stock, int categoryId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentOutOfRangeException.ThrowIfNegative(price);
    ArgumentOutOfRangeException.ThrowIfNegative(stock);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(categoryId);

    Name = name.Trim();
    Price = price;
    Stock = stock;
    CategoryId = categoryId;
  }

  public void AssignCategory(Category category)
  {
    ArgumentNullException.ThrowIfNull(category);
    if(category.Id <= 0) throw new InvalidOperationException("Save Category first");

    CategoryId = category.Id;
    Category = category;
  }
}