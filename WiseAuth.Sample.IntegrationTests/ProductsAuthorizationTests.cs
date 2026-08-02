using System.Net;
using System.Net.Http.Json;
using Shouldly;
using WiseAuth.Sample.Modules.Products;

namespace WiseAuth.Sample.IntegrationTests;

public class ProductsAuthorizationTests
{
    [ClassDataSource<SampleAppFactory>(Shared = SharedType.PerAssembly)]
    public required SampleAppFactory Factory { get; init; }

    [Test]
    public async Task Admin_GetProducts_ReturnsSeededProducts()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);

        var response = await client.GetAsync("/api/products");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<ProductResponse[]>();
        products.ShouldNotBeNull();
        products.ShouldContain(p => p.Sku == "WA-001");
    }

    [Test]
    public async Task Admin_GetProductById_ReturnsProduct()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var productId = await GetSeededProductIdAsync(client);

        var response = await client.GetAsync($"/api/products/{productId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Admin_ExportProducts_ReturnsCsv()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);

        var response = await client.GetAsync("/api/products/export");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/csv");
        var csv = await response.Content.ReadAsStringAsync();
        csv.ShouldStartWith("Id,Sku,Name,Price,Quantity,CreatedUtc");
    }

    [Test]
    public async Task Admin_CreateProduct_ReturnsCreated()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var sku = $"IT-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(sku, "Integration Test Widget", 1.23m, 5));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ProductResponse>();
        created.ShouldNotBeNull();
        created.Sku.ShouldBe(sku);
    }

    [Test]
    public async Task Admin_UpdateProduct_ReturnsOk()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var created = await CreateDedicatedProductAsync(client);

        var response = await client.PutAsJsonAsync($"/api/products/{created.Id}", new UpdateProductRequest(created.Sku, "After Update", 2m, 2));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();
        updated!.Name.ShouldBe("After Update");
    }

    [Test]
    public async Task Admin_DeleteProduct_RemovesProduct()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var created = await CreateDedicatedProductAsync(client);

        var deleteResponse = await client.DeleteAsync($"/api/products/{created.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/products/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Viewer_GetProducts_ReturnsSeededProducts()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword);

        var response = await client.GetAsync("/api/products");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Viewer_GetProductById_ReturnsProduct()
    {
        var adminClient = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var productId = await GetSeededProductIdAsync(adminClient);
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword);

        var response = await client.GetAsync($"/api/products/{productId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    [Arguments("GET", "/api/products/export")]
    [Arguments("POST", "/api/products")]
    [Arguments("PUT", "/api/products/1")]
    [Arguments("DELETE", "/api/products/1")]
    public async Task Viewer_WriteAndExportEndpoints_ReturnForbidden(string method, string route)
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword);

        var response = await client.SendRequestAsync(method, route);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Auditor_GetProducts_ReturnsSeededProducts()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword);

        var response = await client.GetAsync("/api/products");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Auditor_GetProductById_ReturnsProduct()
    {
        var adminClient = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var productId = await GetSeededProductIdAsync(adminClient);
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword);

        var response = await client.GetAsync($"/api/products/{productId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Auditor_ExportProducts_ReturnsCsv()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword);

        var response = await client.GetAsync("/api/products/export");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    [Arguments("POST", "/api/products")]
    [Arguments("PUT", "/api/products/1")]
    [Arguments("DELETE", "/api/products/1")]
    public async Task Auditor_WriteAndDeleteEndpoints_ReturnForbidden(string method, string route)
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword);

        var response = await client.SendRequestAsync(method, route);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<int> GetSeededProductIdAsync(HttpClient adminClient)
    {
        var products = await adminClient.GetFromJsonAsync<ProductResponse[]>("/api/products");
        return products!.First(p => p.Sku == "WA-001").Id;
    }

    // Positive Update/Delete tests act on a throwaway product created here rather than a seeded
    // one, so they stay safe to run in parallel with everything else without racing over shared rows.
    private static async Task<ProductResponse> CreateDedicatedProductAsync(HttpClient adminClient)
    {
        var sku = $"IT-{Guid.NewGuid():N}";
        var response = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(sku, "Dedicated Test Product", 1m, 1));
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }
}
