using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Merchant;

namespace TakeoutPlatform.Api.Tests;

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "test-only-jwt-signing-key-with-at-least-32-bytes";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ApiTestFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(
                service => service.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public HttpClient CreateAuthenticatedClient(AccountRole role, int profileId, int accountId = 100)
    {
        var client = CreateClient();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, accountId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, accountId.ToString()),
            new Claim("role", role.ToString()),
            new Claim(ClaimsPrincipalExtensions.ProfileIdClaim, profileId.ToString()),
        };
        var token = new JwtSecurityToken(
            "TakeoutPlatform",
            "TakeoutPlatform.Client",
            claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
                SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    public void EnsureDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        db.Merchants.Add(new Merchant
        {
            Id = 2,
            Name = "Second Test Merchant",
        });
        db.SaveChanges();
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
