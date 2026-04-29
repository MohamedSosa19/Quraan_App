using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quraan.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace Quraan.IntegrationTests.Fixtures;

/// <summary>
/// Shared xUnit collection fixture that spins up a real SQL Server 2022
/// container via Testcontainers and exposes a <see cref="WebApplicationFactory"/>
/// pointed at it. Principle III: integration tests MUST use a real DB.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Local-Dev-Password-1!")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = default!;
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = ConnectionString,
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<QuraanDbContext>));
                services.AddDbContext<QuraanDbContext>(o => o.UseSqlServer(ConnectionString));
            });
        });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuraanDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync().ConfigureAwait(false);
        await _container.DisposeAsync().ConfigureAwait(false);
    }
}

[CollectionDefinition(nameof(SqlServerCollection))]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture> { }
