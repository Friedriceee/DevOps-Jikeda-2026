using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Admin;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// The built-in administrator must not be a default credential. A password committed to the
/// repository (the old "Admin@12345") would be a working login on every database created
/// from the migrations.
/// </summary>
public class AdminBootstrapTests
{
    private const string StrongPassword = "A-long-operator-chosen-secret-1";
    private SqliteConnection _connection = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        db.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown() => _connection.Dispose();

    private ServiceProvider Services(string? initialPassword)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Options.Create(new AdminBootstrapOptions { InitialPassword = initialPassword }));
        // test-only signing key for an in-memory host; it protects nothing
        services.AddSingleton(Options.Create(new JwtOptions { Key = new string('k', 48) }));
        services.AddScoped<AuthService>();
        return services.BuildServiceProvider();
    }

    private static async Task<LoginResponse?> SignInAsAdmin(ServiceProvider provider, string password)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AuthService>()
            .LoginAsync(new LoginRequest { Username = "admin", Password = password }, default);
    }

    [TestCase("Admin@12345")]
    [TestCase("admin")]
    [TestCase("password")]
    [TestCase("")]
    public async Task The_built_in_admin_cannot_sign_in_with_any_password_until_it_is_activated(string password)
    {
        await using var provider = Services(initialPassword: null);

        Assert.That(await SignInAsAdmin(provider, password), Is.Null);
    }

    [Test]
    public async Task The_seeded_administrator_row_carries_no_usable_password()
    {
        await using var provider = Services(initialPassword: null);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var admin = await db.Accounts.SingleAsync(account => account.Id == 1000);

        Assert.That(admin.Role, Is.EqualTo(AccountRole.Administrator));
        Assert.That(admin.PasswordHash, Is.EqualTo(PasswordHashes.Unusable));
    }

    [Test]
    public async Task An_operator_supplied_initial_password_activates_the_admin()
    {
        await using var provider = Services(StrongPassword);

        await AdminBootstrap.RunAsync(provider);

        Assert.That(await SignInAsAdmin(provider, StrongPassword), Is.Not.Null);
        Assert.That(await SignInAsAdmin(provider, "Admin@12345"), Is.Null);
    }

    [TestCase("short")]
    [TestCase("Admin@12345")] // 11 characters: below the minimum, and the old committed password
    public void A_weak_initial_password_is_refused_at_start_up(string password)
    {
        using var provider = Services(password);

        Assert.ThrowsAsync<InvalidOperationException>((Func<Task>)(() => AdminBootstrap.RunAsync(provider)));
    }

    [Test]
    public async Task Start_up_does_nothing_when_no_initial_password_is_configured()
    {
        await using var provider = Services(initialPassword: null);

        await AdminBootstrap.RunAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var admin = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Accounts.SingleAsync(a => a.Id == 1000);
        Assert.That(admin.PasswordHash, Is.EqualTo(PasswordHashes.Unusable));
    }

    [Test]
    public async Task A_password_that_was_already_set_is_never_overwritten()
    {
        await using (var first = Services(StrongPassword))
            await AdminBootstrap.RunAsync(first);

        await using var second = Services("Another-secret-that-must-not-win-2");
        await AdminBootstrap.RunAsync(second);

        Assert.That(await SignInAsAdmin(second, StrongPassword), Is.Not.Null);
        Assert.That(await SignInAsAdmin(second, "Another-secret-that-must-not-win-2"), Is.Null);
    }
}
