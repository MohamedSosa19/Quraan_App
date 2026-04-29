using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Quraan.ContractTests;

/// <summary>
/// T071a — contract test for /api/v1/health/live and /api/v1/health/ready.
/// Resolves analyzer C1: Constitution III mandates a contract test per HTTP
/// endpoint. Runs the host in Development env so the boot-time content
/// integrity gate stays lenient (no real seed sources are needed for this
/// test to assert the wire contract).
/// </summary>
public class HealthContractTests : IClassFixture<HealthContractTests.DevFactory>
{
    public sealed class DevFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
        }
    }

    private readonly DevFactory _factory;
    public HealthContractTests(DevFactory factory) => _factory = factory;

    [Fact]
    public async Task Live_returns_200_with_status_live()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/health/live", UriKind.Relative));
        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("live");
    }

    [Fact]
    public async Task Ready_endpoint_is_reachable()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/health/ready", UriKind.Relative));
        ((int)response.StatusCode).Should().BeOneOf(200, 503);
    }
}
