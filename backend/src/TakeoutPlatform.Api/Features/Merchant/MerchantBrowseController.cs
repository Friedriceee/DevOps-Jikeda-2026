using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Data;

namespace TakeoutPlatform.Api.Features.Merchant;

/// <summary>Public restaurant directory used by the customer menu browser.</summary>
[ApiController]
[Route("api/merchants")]
public sealed class MerchantBrowseController : ControllerBase
{
    private readonly AppDbContext _db;

    public MerchantBrowseController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ApiResult<IReadOnlyList<MerchantBrowseResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var merchants = await _db.Merchants
            .AsNoTracking()
            .OrderBy(merchant => merchant.Name)
            .Select(merchant => new MerchantBrowseResponse(
                merchant.Id,
                merchant.Name,
                merchant.Address,
                merchant.Contact,
                merchant.DishType,
                merchant.TimeForOpenBusiness,
                merchant.TimeForCloseBusiness))
            .ToListAsync(cancellationToken);

        return Ok(ApiResult<IReadOnlyList<MerchantBrowseResponse>>.Ok(merchants));
    }
}

public sealed record MerchantBrowseResponse(
    int Id,
    string Name,
    string? Address,
    string? Contact,
    string? DishType,
    int? TimeForOpenBusiness,
    int? TimeForCloseBusiness);
