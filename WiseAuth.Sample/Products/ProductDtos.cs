namespace WiseAuth.Sample.Products;

public record ProductResponse(int Id, string Sku, string Name, decimal Price, int Quantity, DateTime CreatedUtc)
{
    public static ProductResponse FromEntity(Product product) =>
        new(product.Id, product.Sku, product.Name, product.Price, product.Quantity, product.CreatedUtc);
}

public record CreateProductRequest(string Sku, string Name, decimal Price, int Quantity);

public record UpdateProductRequest(string Sku, string Name, decimal Price, int Quantity);
