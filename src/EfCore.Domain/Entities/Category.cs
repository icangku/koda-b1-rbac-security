namespace EfCore.Domain.Entities;

public sealed class Category
{
  public int Id {get; set;}
  public string Name {get; set;} = null!;
  public ICollection<Product> Products {get; private set;} = [];

  public Category(string name)
  {
    if(string.IsNullOrWhiteSpace(name) || name.Trim().Length > 30)
    {
      throw new ArgumentException("Category name should be filled, maximum 30 characters", nameof(name));
    }
    Name = name.Trim();
  }
}