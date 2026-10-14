using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using WiseAuth.Sample.Modules.Products;

namespace WiseAuth.Sample.IntegrationTests;

public class AllowAnonymousTests
{
    [ClassDataSource<SampleAppFactory>(Shared = SharedType.PerAssembly)]
    public required SampleAppFactory Factory { get; init; }

    [Test]
    public async Task EndpointId_WithAllowAnonymous_AllowsUnauthenticatedRequest()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/integration-tests/allow-anonymous");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

[ApiController]
[Route("api/integration-tests")]
public sealed class AllowAnonymousTestController : ControllerBase
{
    [HttpGet("allow-anonymous")]
    [EndpointId<ProductPermissions>(ProductPermissions.Read)]
    [AllowAnonymous]
    public IActionResult Get() => Ok();
}
