using System.Net;
using Shouldly;

namespace WiseAuth.Sample.IntegrationTests;

public class HealthCheckTests
{
    [ClassDataSource<SampleAppFactory>(Shared = SharedType.PerAssembly)]
    public required SampleAppFactory Factory { get; init; }

    [Test]
    public async Task Get_PermissionsSchema_ReturnsOk()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/auth/permissions-schema");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
