using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Rider;

public sealed class RegisterRiderRequest
{
    [Required, StringLength(50, MinimumLength = 3)] public string Username { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    [Required, StringLength(50)] public string Name { get; set; } = string.Empty;
    [Required, RegularExpression(@"^\d{8,11}$")] public string PhoneNumber { get; set; } = string.Empty;
    [Required, StringLength(50)] public string Zone { get; set; } = "ZoneA";
    [Range(1, 20)] public int Capacity { get; set; } = 3;
}

public sealed record RiderRegistrationResponse(int Id, string Username, string Name, string Zone);

public sealed class RiderRegistrationService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Account> _hasher;
    private readonly TimeProvider _timeProvider;

    public RiderRegistrationService(AppDbContext db, IPasswordHasher<Account> hasher, TimeProvider timeProvider)
    {
        _db = db;
        _hasher = hasher;
        _timeProvider = timeProvider;
    }

    public async Task<(bool Conflict, RiderRegistrationResponse? Rider)> RegisterAsync(
        RegisterRiderRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var normalized = AuthService.NormalizeUsername(username);
        if (await _db.Accounts.AnyAsync(item => item.NormalizedUsername == normalized, cancellationToken))
            return (true, null);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var rider = new Rider
        {
            Name = request.Name.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Zone = request.Zone.Trim(),
            Capacity = request.Capacity,
            IsActive = true,
            IsAvailable = true,
        };
        _db.Riders.Add(rider);
        await _db.SaveChangesAsync(cancellationToken);

        var account = new Account
        {
            Username = username,
            NormalizedUsername = normalized,
            Role = AccountRole.Rider,
            ProfileId = rider.Id,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            IsActive = true,
        };
        account.PasswordHash = _hasher.HashPassword(account, request.Password);
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

        return (false, new RiderRegistrationResponse(rider.Id, username, rider.Name, rider.Zone));
    }
}

[ApiController]
[Route("api/rider")]
public sealed class RiderRegistrationController : ControllerBase
{
    private readonly RiderRegistrationService _service;
    public RiderRegistrationController(RiderRegistrationService service) => _service = service;

    [HttpPost("register")]
    public async Task<ActionResult<ApiResult<RiderRegistrationResponse>>> Register(
        [FromBody] RegisterRiderRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.RegisterAsync(request, cancellationToken);
        return result.Conflict
            ? Conflict(ApiResult<RiderRegistrationResponse>.Fail("Username already exists."))
            : StatusCode(StatusCodes.Status201Created, ApiResult<RiderRegistrationResponse>.Ok(result.Rider!));
    }
}
