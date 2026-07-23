namespace WiseAuth.Sample.Modules.Products;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
