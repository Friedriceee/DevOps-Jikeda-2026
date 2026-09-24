using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Tests;

public class AuthServiceTests : ServiceTestBase
{
    private static readonly JwtOptions Jwt = new()
    {
        Issuer = "TakeoutPlatform.Tests",
        Audience = "TakeoutPlatform.Tests.Client",
        Key = ApiTestFactory.JwtKey,
        ExpiryMinutes = 30,
    };

    [Test]
    public async Task Register_customer_creates_profile_and_hashed_account()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var result = await service.RegisterCustomerAsync(new RegisterCustomerRequest
        {
            Username = "alice",
            Password = "SecurePassword123",
            DisplayName = "Alice",
            PhoneNumber = "13800000001",
        }, CancellationToken.None);

        Assert.That(result.Conflict, Is.False);
        var account = db.Accounts.Single(a => a.Username == "alice");
        Assert.That(account.Role, Is.EqualTo(AccountRole.Customer));
        Assert.That(account.ProfileId, Is.EqualTo(result.Customer!.CustomerId));
        Assert.That(account.PasswordHash, Is.Not.EqualTo("SecurePassword123"));
        Assert.That(new PasswordHasher<Account>().VerifyHashedPassword(
            account, account.PasswordHash, "SecurePassword123"),
            Is.Not.EqualTo(PasswordVerificationResult.Failed));
    }

    [Test]
    public async Task Register_customer_rejects_case_insensitive_duplicate_username()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var first = NewCustomer("Alice");
        var second = NewCustomer(" alice ");

        Assert.That((await service.RegisterCustomerAsync(first, CancellationToken.None)).Conflict, Is.False);
        Assert.That((await service.RegisterCustomerAsync(second, CancellationToken.None)).Conflict, Is.True);
        Assert.That(db.Accounts.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Login_returns_signed_token_with_account_role_and_profile_claims()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        await service.RegisterCustomerAsync(NewCustomer("alice"), CancellationToken.None);

        var login = await service.LoginAsync(new LoginRequest
        {
            Username = "ALICE",
            Password = "SecurePassword123",
        }, CancellationToken.None);

        Assert.That(login, Is.Not.Null);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(login!.AccessToken);
        Assert.That(login.Role, Is.EqualTo("Customer"));
        Assert.That(token.Claims.Single(c => c.Type == ClaimsPrincipalExtensions.ProfileIdClaim).Value,
            Is.EqualTo(login.ProfileId.ToString()));
        Assert.That(token.Claims.Single(c => c.Type == "role").Value, Is.EqualTo("Customer"));
    }

    [Test]
    public async Task Login_rejects_wrong_password()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        await service.RegisterCustomerAsync(NewCustomer("alice"), CancellationToken.None);

        var login = await service.LoginAsync(new LoginRequest
        {
            Username = "alice",
            Password = "wrong-password",
        }, CancellationToken.None);

        Assert.That(login, Is.Null);
    }

    private static RegisterCustomerRequest NewCustomer(string username) => new()
    {
        Username = username,
        Password = "SecurePassword123",
        DisplayName = "Alice",
        PhoneNumber = "13800000001",
    };

    private static AuthService CreateService(Data.AppDbContext db) => new(
        db,
        new PasswordHasher<Account>(),
        Options.Create(Jwt),
        TimeProvider.System);
}
