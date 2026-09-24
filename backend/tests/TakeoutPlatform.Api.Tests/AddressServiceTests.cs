using NUnit.Framework;
using TakeoutPlatform.Api.Features.User;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// AddressService 的单元测试（地址新增/查询）。
/// 参照旧项目 SubmitAddress / GetAddress 的逻辑：
/// 校验用户存在、字段规整（Trim / 空白 HouseNumber 转 null）、按用户隔离。
/// 种子数据中已有用户 Id=1（Demo User，来自 AppDbContext.HasData）。
/// </summary>
public class AddressServiceTests : ServiceTestBase
{
    private const int SeededUserId = 1;
    private const int MissingUserId = 999;

    private static CreateAddressRequest NewRequest(
        int userId = SeededUserId,
        string? address = "No.1 Main Street",
        string? houseNumber = "101",
        string? contactName = "Alice",
        string? phoneNumber = "13800000000") => new()
    {
        UserId = userId,
        Address = address,
        HouseNumber = houseNumber,
        ContactName = contactName,
        PhoneNumber = phoneNumber,
    };

    [Test]
    public async Task CreateAsync_for_existing_user_returns_address_and_persists_it()
    {
        await using var context = CreateContext();
        var service = new AddressService(context);

        var (userFound, address) = await service.CreateAsync(NewRequest(), CancellationToken.None);

        Assert.That(userFound, Is.True);
        Assert.That(address, Is.Not.Null);
        Assert.That(address!.Id, Is.GreaterThan(0));
        Assert.That(address.UserId, Is.EqualTo(SeededUserId));
        Assert.That(address.Address, Is.EqualTo("No.1 Main Street"));
        Assert.That(address.ContactName, Is.EqualTo("Alice"));

        await using var verifyContext = CreateContext();
        Assert.That(verifyContext.UserAddresses.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateAsync_for_missing_user_returns_not_found_and_persists_nothing()
    {
        await using var context = CreateContext();
        var service = new AddressService(context);

        var (userFound, address) = await service.CreateAsync(
            NewRequest(userId: MissingUserId),
            CancellationToken.None);

        Assert.That(userFound, Is.False);
        Assert.That(address, Is.Null);

        await using var verifyContext = CreateContext();
        Assert.That(verifyContext.UserAddresses.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task CreateAsync_trims_fields_and_converts_blank_house_number_to_null()
    {
        await using var context = CreateContext();
        var service = new AddressService(context);

        var (_, address) = await service.CreateAsync(
            NewRequest(address: "  No.2 Side Road  ", houseNumber: "   ", contactName: "  Bob  "),
            CancellationToken.None);

        Assert.That(address!.Address, Is.EqualTo("No.2 Side Road"));
        Assert.That(address.HouseNumber, Is.Null);
        Assert.That(address.ContactName, Is.EqualTo("Bob"));
    }

    [Test]
    public async Task ListByUserAsync_for_missing_user_returns_null()
    {
        await using var context = CreateContext();
        var service = new AddressService(context);

        var addresses = await service.ListByUserAsync(MissingUserId, CancellationToken.None);

        Assert.That(addresses, Is.Null);
    }

    [Test]
    public async Task ListByUserAsync_returns_empty_list_when_user_has_no_address()
    {
        await using var context = CreateContext();
        var service = new AddressService(context);

        var addresses = await service.ListByUserAsync(SeededUserId, CancellationToken.None);

        Assert.That(addresses, Is.Not.Null);
        Assert.That(addresses, Is.Empty);
    }

    [Test]
    public async Task ListByUserAsync_returns_only_that_users_addresses_ordered_by_id()
    {
        await using (var seedContext = CreateContext())
        {
            var seedService = new AddressService(seedContext);
            await seedService.CreateAsync(NewRequest(address: "First"), CancellationToken.None);
            await seedService.CreateAsync(NewRequest(address: "Second"), CancellationToken.None);
        }

        await using var context = CreateContext();
        var service = new AddressService(context);

        var addresses = await service.ListByUserAsync(SeededUserId, CancellationToken.None);

        Assert.That(addresses, Is.Not.Null);
        Assert.That(addresses, Has.Count.EqualTo(2));
        Assert.That(addresses![0].Address, Is.EqualTo("First"));
        Assert.That(addresses![1].Address, Is.EqualTo("Second"));
    }
}
