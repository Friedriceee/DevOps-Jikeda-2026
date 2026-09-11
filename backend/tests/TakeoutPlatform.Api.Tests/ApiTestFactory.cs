using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TakeoutPlatform.Api.Data;

namespace TakeoutPlatform.Api.Tests;

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ApiTestFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(
                service => service.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public void EnsureDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }
}
