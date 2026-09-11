using NUnit.Framework;
using TakeoutPlatform.Api.Features.Merchant;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// DishService 的单元测试（US-01~US-04：菜品的增查改删）。
///
/// 只测 Service 层本身的职责：商家存在性判断、数据落库、按商家隔离、字段规整（Trim / 空白转 null）。
/// 请求字段的格式校验（价格必须大于 0、名称长度等）由控制器层的模型校验负责，
/// 属于接口测试的范畴，不在单元测试里重复。
/// </summary>
public class DishServiceTests : ServiceTestBase
{
    private static CreateDishRequest NewCreateRequest(
        int merchantId = MerchantId,
        string? name = "Kung Pao Chicken",
        decimal price = 28.00m,
        string? category = "Sichuan",
        string? imageUrl = null,
        int inventory = 100) => new()
    {
        MerchantId = merchantId,
        Name = name,
        Price = price,
        Category = category,
        ImageUrl = imageUrl,
        Inventory = inventory,
    };

    // ---------- CreateAsync（US-01：商家创建菜品）----------

    [Test]
    public async Task CreateAsync_for_existing_merchant_returns_dish_and_persists_it()
    {
        await using var context = CreateContext();
        var service = new DishService(context);

        var (merchantFound, dish) = await service.CreateAsync(NewCreateRequest(), CancellationToken.None);

        Assert.That(merchantFound, Is.True);
        Assert.That(dish, Is.Not.Null);
        Assert.That(dish!.Id, Is.GreaterThan(0));
        Assert.That(dish.MerchantId, Is.EqualTo(MerchantId));
        Assert.That(dish.Name, Is.EqualTo("Kung Pao Chicken"));
        Assert.That(dish.Price, Is.EqualTo(28.00m));
        Assert.That(dish.Inventory, Is.EqualTo(100));

        // 换一个 context 读取，确认数据真的写进了数据库。
        await using var verifyContext = CreateContext();
        Assert.That(verifyContext.Dishes.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateAsync_for_missing_merchant_returns_not_found_and_persists_nothing()
    {
        await using var context = CreateContext();
        var service = new DishService(context);

        var (merchantFound, dish) = await service.CreateAsync(
            NewCreateRequest(merchantId: MissingMerchantId),
            CancellationToken.None);

        Assert.That(merchantFound, Is.False);
        Assert.That(dish, Is.Null);

        await using var verifyContext = CreateContext();
        Assert.That(verifyContext.Dishes.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task CreateAsync_trims_name_and_converts_blank_optional_fields_to_null()
    {
        await using var context = CreateContext();
        var service = new DishService(context);

        var (_, dish) = await service.CreateAsync(
            NewCreateRequest(name: "  Fried Rice  ", category: "   ", imageUrl: "   "),
            CancellationToken.None);

        Assert.That(dish!.Name, Is.EqualTo("Fried Rice"));
        Assert.That(dish.Category, Is.Null);
        Assert.That(dish.ImageUrl, Is.Null);
    }

    // ---------- ListAsync（US-02：商家查看菜品列表）----------

    [Test]
    public async Task ListAsync_for_missing_merchant_returns_null()
    {
        await using var context = CreateContext();
        var service = new DishService(context);

        var dishes = await service.ListAsync(MissingMerchantId, CancellationToken.None);

        Assert.That(dishes, Is.Null);
    }

    [Test]
    public async Task ListAsync_returns_empty_list_when_merchant_has_no_dishes()
    {
        await using var context = CreateContext();
        var service = new DishService(context);

        var dishes = await service.ListAsync(MerchantId, CancellationToken.None);

        Assert.That(dishes, Is.Not.Null);
        Assert.That(dishes, Is.Empty);
    }

    [Test]
    public async Task ListAsync_returns_only_the_requested_merchants_dishes_ordered_by_id()
    {
        await using (var seedContext = CreateContext())
        {
            var seedService = new DishService(seedContext);
            await seedService.CreateAsync(NewCreateRequest(name: "First"), CancellationToken.None);
            await seedService.CreateAsync(NewCreateRequest(name: "Second"), CancellationToken.None);
            await seedService.CreateAsync(
                NewCreateRequest(merchantId: OtherMerchantId, name: "Other Merchant Dish"),
                CancellationToken.None);
        }

        await using var context = CreateContext();
        var service = new DishService(context);

        var dishes = await service.ListAsync(MerchantId, CancellationToken.None);

        Assert.That(dishes, Is.Not.Null);
        Assert.That(dishes, Has.Count.EqualTo(2));
        Assert.That(dishes!.All(d => d.MerchantId == MerchantId), Is.True);
        Assert.That(dishes![0].Name, Is.EqualTo("First"));
        Assert.That(dishes![1].Name, Is.EqualTo("Second"));
    }

    // ---------- UpdateAsync（US-03：商家编辑菜品）----------

    [Test]
    public async Task UpdateAsync_changes_fields_and_persists()
    {
        int dishId;
        await using (var seedContext = CreateContext())
        {
            var seedService = new DishService(seedContext);
            var (_, created) = await seedService.CreateAsync(NewCreateRequest(), CancellationToken.None);
            dishId = created!.Id;
        }

        await using var context = CreateContext();
        var service = new DishService(context);

        var updated = await service.UpdateAsync(
            dishId,
            MerchantId,
            new UpdateDishRequest
            {
                Name = "  Updated Dish  ",
                Price = 12.50m,
                Category = "Special",
                ImageUrl = "https://example.com/dish.png",
                Inventory = 20,
            },
            CancellationToken.None);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Name, Is.EqualTo("Updated Dish"));
        Assert.That(updated.Price, Is.EqualTo(12.50m));
        Assert.That(updated.Inventory, Is.EqualTo(20));

        await using var verifyContext = CreateContext();
        var persisted = verifyContext.Dishes.Single(d => d.Id == dishId);
        Assert.That(persisted.Name, Is.EqualTo("Updated Dish"));
        Assert.That(persisted.Inventory, Is.EqualTo(20));
    }

    [Test]
    public async Task UpdateAsync_for_another_merchant_returns_null_and_leaves_data_unchanged()
    {
        int dishId;
        await using (var seedContext = CreateContext())
        {
            var seedService = new DishService(seedContext);
            var (_, created) = await seedService.CreateAsync(NewCreateRequest(name: "Protected"), CancellationToken.None);
            dishId = created!.Id;
        }

        await using var context = CreateContext();
        var service = new DishService(context);

        var updated = await service.UpdateAsync(
            dishId,
            OtherMerchantId,
            new UpdateDishRequest { Name = "Hacked", Price = 1.00m, Inventory = 0 },
            CancellationToken.None);

        Assert.That(updated, Is.Null);

        await using var verifyContext = CreateContext();
        Assert.That(verifyContext.Dishes.Single(d => d.Id == dishId).Name, Is.EqualTo("Protected"));
    }

    [Test]
    public async Task UpdateAsync_for_missing_dish_returns_null()
    {
        await using var context = CreateContext();
        var service = new DishService(context);

        var updated = await service.UpdateAsync(
            999,
            MerchantId,
            new UpdateDishRequest { Name = "Nope", Price = 1.00m, Inventory = 0 },
            CancellationToken.None);

        Assert.That(updated, Is.Null);
    }

    // ---------- DeleteAsync（US-04：商家下架/删除菜品）----------

    [Test]
    public async Task DeleteAsync_removes_the_dish_and_returns_true()
    {
        int dishId;
        await using (var seedContext = CreateContext())
        {
            var seedService = new DishService(seedContext);
            var (_, created) = await seedService.CreateAsync(NewCreateRequest(), CancellationToken.None);
            dishId = created!.Id;
        }

        await using var context = CreateContext();
        var service = new DishService(context);

        var deleted = await service.DeleteAsync(dishId, MerchantId, CancellationToken.None);

        Assert.That(deleted, Is.True);

        await using var verifyContext = CreateContext();
        Assert.That(verifyContext.Dishes.Any(d => d.Id == dishId), Is.False);
    }

    [Test]
    public async Task DeleteAsync_for_another_merchant_returns_false_and_keeps_the_dish()
    {
        int dishId;
        await using (var seedContext = CreateContext())
        {
            var seedService = new DishService(seedContext);
            var (_, created) = await seedService.CreateAsync(NewCreateRequest(), CancellationToken.None);
            dishId = created!.Id;
        }

        await using var context = CreateContext();
        var service = new DishService(context);

        var deleted = await service.DeleteAsync(dishId, OtherMerchantId, CancellationToken.None);

        Assert.That(deleted, Is.False);

        await using var verifyContext = CreateContext();
        Assert.That(verifyContext.Dishes.Any(d => d.Id == dishId), Is.True);
    }

    [Test]
    public async Task DeleteAsync_for_missing_dish_returns_false()
    {
        await using var context = CreateContext();
        var service = new DishService(context);

        var deleted = await service.DeleteAsync(999, MerchantId, CancellationToken.None);

        Assert.That(deleted, Is.False);
    }
}
