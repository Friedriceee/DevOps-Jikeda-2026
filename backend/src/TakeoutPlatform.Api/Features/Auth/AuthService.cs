using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TakeoutPlatform.Api.Data;
using Customer = TakeoutPlatform.Api.Features.User.User;

namespace TakeoutPlatform.Api.Features.Auth;

public sealed class AuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Account> _passwordHasher;
    private readonly JwtOptions _jwt;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        AppDbContext db,
        IPasswordHasher<Account> passwordHasher,
        IOptions<JwtOptions> jwt,
        TimeProvider timeProvider)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt.Value;
        _timeProvider = timeProvider;
    }

    public async Task<(bool Conflict, CustomerRegistrationResponse? Customer)> RegisterCustomerAsync(
        RegisterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var normalizedUsername = NormalizeUsername(username);
        if (await _db.Accounts.AnyAsync(a => a.NormalizedUsername == normalizedUsername, cancellationToken))
            return (true, null);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var customer = new Customer
        {
            UserName = request.DisplayName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Wallet = 0,
        };
        _db.Users.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        var account = new Account
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            Role = AccountRole.Customer,
            ProfileId = customer.Id,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        };
        account.PasswordHash = _passwordHasher.HashPassword(account, request.Password);
        _db.Accounts.Add(account);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (true, null);
        }

        return (false, new CustomerRegistrationResponse(
            account.Id, customer.Id, account.Username, customer.UserName));
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = NormalizeUsername(request.Username);
        var account = await _db.Accounts.SingleOrDefaultAsync(
            a => a.NormalizedUsername == normalizedUsername,
            cancellationToken);
        if (account is null)
            return null;

        var verification = _passwordHasher.VerifyHashedPassword(
            account, account.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            return null;

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = _passwordHasher.HashPassword(account, request.Password);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_jwt.ExpiryMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Username),
            new Claim("role", account.Role.ToString()),
            new Claim(ClaimsPrincipalExtensions.ProfileIdClaim, account.ProfileId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expires,
            account.Id,
            account.Role.ToString(),
            account.ProfileId);
    }

    public static string NormalizeUsername(string username) => username.Trim().ToUpperInvariant();
}
