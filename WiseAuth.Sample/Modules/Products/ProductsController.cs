using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Products;

// Attribute-routed ApiController equivalent of the other areas' minimal-API MapXEndpoints
// pattern, protected with [EndpointId<T>] instead of the minimal API's .EndpointId() extension.
[ApiController]
[Route("api/products")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [EndpointId<ProductPermissions>(ProductPermissions.Read)]
    public async Task<IActionResult> GetAll()
    {
        var products = await db.Products.OrderBy(p => p.Name).ToListAsync();
        return Ok(products.Select(ProductResponse.FromEntity));
    }

    [HttpGet("export")]
    [EndpointId<ProductPermissions>(ProductPermissions.Export)]
    public async Task<IActionResult> Export()
    {
        var products = await db.Products.OrderBy(p => p.Name).ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,Sku,Name,Price,Quantity,CreatedUtc");
        foreach (var product in products)
        {
            csv.AppendLine(string.Join(',',
                product.Id.ToString(),
                CsvField(product.Sku),
                CsvField(product.Name),
                product.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
                product.Quantity.ToString(),
                product.CreatedUtc.ToString("O")));
        }

        return Content(csv.ToString(), "text/csv");
    }

    [HttpGet("{id:int}")]
    [EndpointId<ProductPermissions>(ProductPermissions.Read)]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return Ok(ProductResponse.FromEntity(product));
    }

    [HttpPost]
    [EndpointId<ProductPermissions>(ProductPermissions.Write)]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        var validationErrors = await ValidateProductAsync(request.Sku, request.Name, request.Price, request.Quantity, excludingId: null);
        if (validationErrors is not null)
        {
            return BadRequest(validationErrors);
        }

        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Price = request.Price,
            Quantity = request.Quantity,
        };
        db.Products.Add(product);

        // The AnyAsync check above is a TOCTOU race - two concurrent creates for the
        // same Sku can both pass it before either saves. The unique index on Sku
        // (AppDbContext) is what actually enforces uniqueness; this just turns the
        // resulting constraint-violation exception into the same clean 400 shape
        // instead of a 500.
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new[] { $"Sku '{request.Sku}' is already in use." });
        }

        return Created($"/api/products/{product.Id}", ProductResponse.FromEntity(product));
    }

    [HttpPut("{id:int}")]
    [EndpointId<ProductPermissions>(ProductPermissions.Write)]
    public async Task<IActionResult> Update(int id, UpdateProductRequest request)
    {
        var validationErrors = await ValidateProductAsync(request.Sku, request.Name, request.Price, request.Quantity, excludingId: id);
        if (validationErrors is not null)
        {
            return BadRequest(validationErrors);
        }

        var product = await db.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        product.Sku = request.Sku;
        product.Name = request.Name;
        product.Price = request.Price;
        product.Quantity = request.Quantity;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new[] { $"Sku '{request.Sku}' is already in use." });
        }

        return Ok(ProductResponse.FromEntity(product));
    }

    [HttpDelete("{id:int}")]
    [EndpointId<ProductPermissions>(ProductPermissions.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<List<string>?> ValidateProductAsync(string sku, string name, decimal price, int quantity, int? excludingId)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(sku))
        {
            errors.Add("Sku is required.");
        }
        // excludingId is the product's own id on an update, so its own row doesn't
        // count as a collision against itself.
        else if (await db.Products.AnyAsync(p => p.Sku == sku && (excludingId == null || p.Id != excludingId)))
        {
            errors.Add($"Sku '{sku}' is already in use.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Name is required.");
        }

        if (price < 0)
        {
            errors.Add("Price cannot be negative.");
        }

        if (quantity < 0)
        {
            errors.Add("Quantity cannot be negative.");
        }

        return errors.Count > 0 ? errors : null;
    }

    // Quotes a CSV field and escapes embedded quotes whenever the value contains a
    // comma, quote, or newline - without this, a Name like "Widget, Deluxe" splits
    // into two columns and shifts every field after it out of alignment.
    private static string CsvField(string value)
    {
        if (value.IndexOfAny([',', '"', '\n', '\r']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
