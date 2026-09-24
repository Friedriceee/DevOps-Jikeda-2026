using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Data;

namespace TakeoutPlatform.Api.Features.User;

/// <summary>
/// 地址服务。对应旧项目 UserController 中的 SubmitAddress / GetAddress 逻辑，
/// 下沉到 Service 层，Controller 只负责 HTTP。
/// </summary>
public sealed class AddressService
{
    private readonly AppDbContext _db;

    public AddressService(AppDbContext db) => _db = db;

    /// <summary>
    /// 新增地址。参照旧 SubmitAddress：先确认用户存在，再落库。
    /// 返回值 UserFound 为 false 表示用户不存在。
    /// </summary>
    public async Task<(bool UserFound, AddressResponse? Address)> CreateAsync(
        CreateAddressRequest request,
        CancellationToken cancellationToken)
    {
        var userExists = await _db.Users
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);

        if (!userExists)
        {
            return (false, null);
        }

        var address = new UserAddress
        {
            UserId = request.UserId,
            Address = request.Address!.Trim(),
            HouseNumber = string.IsNullOrWhiteSpace(request.HouseNumber) ? null : request.HouseNumber.Trim(),
            ContactName = request.ContactName!.Trim(),
            PhoneNumber = request.PhoneNumber!.Trim(),
        };

        _db.UserAddresses.Add(address);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, ToResponse(address));
    }

    /// <summary>
    /// 按用户查询地址列表。参照旧 GetAddress。
    /// 用户不存在返回 null，用于区分"用户不存在"与"用户暂无地址"。
    /// </summary>
    public async Task<IReadOnlyList<AddressResponse>?> ListByUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var userExists = await _db.Users
            .AnyAsync(user => user.Id == userId, cancellationToken);

        if (!userExists)
        {
            return null;
        }

        return await _db.UserAddresses
            .AsNoTracking()
            .Where(address => address.UserId == userId)
            .OrderBy(address => address.Id)
            .Select(address => new AddressResponse(
                address.Id,
                address.UserId,
                address.Address,
                address.HouseNumber,
                address.ContactName,
                address.PhoneNumber))
            .ToListAsync(cancellationToken);
    }

    private static AddressResponse ToResponse(UserAddress address) => new(
        address.Id,
        address.UserId,
        address.Address,
        address.HouseNumber,
        address.ContactName,
        address.PhoneNumber);
}
