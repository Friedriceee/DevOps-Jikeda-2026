using NUnit.Framework;
using TakeoutPlatform.Api.Features.Merchant;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// SpecialOfferService 的单元测试（US-05~US-07：满减活动的新增、编辑/删除、查看）。
///
/// 与 DishServiceTests 同理，只测 Service 层职责：商家存在性、数据落库、按商家隔离。
/// 满减金额的合法性校验（minPrice/amountRemission 的取值、精度、大小关系）由控制器层
/// 的模型校验负责，属于接口测试范畴，这里不重复。
/// </summary>
public class SpecialOfferServiceTests : ServiceTestBase
{
    private static CreateSpecialOfferRequest NewCreateRequest(
        int merchantId = MerchantId,
        decimal minPrice = 50.00m,
        decimal amountRemission = 5.00m) => new()
    {
        MerchantId = merchantId,
        MinPrice = minPrice,
        AmountRemission = amountRemission,
    };

    // ---------- CreateAsync（US-05：商家新增满减活动）----------

    [Test]
    public async Task CreateAsync_for_existing_merchant_returns_offer_and_persists_it()
    {
        await using var context = CreateContext();
        var service = new SpecialOfferService(context);

        var (merchantFound, offer) = await service.CreateAsync(NewCreateRequest(), CancellationToken.None);

        Assert.That(merchantFound, Is.True);
        Assert.That(offer, Is.Not.Null);
        Assert.That(offer!.Id, Is.GreaterThan(0));
        Assert.That(offer.MerchantId, Is.EqualTo(MerchantId));
        Assert.That(offer.MinPrice, Is.EqualTo(50.00m));
        Assert.That(offer.AmountRemission, Is.EqualTo(5.00m));

        await using var verifyContext = CreateContext();
        Assert.That(verifyContext.SpecialOffers.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateAsync_for_missing_merchant_returns_not_found_and_persists_nothing()
    {
        await using var context = CreateContext();
        var service = new SpecialOfferService(context);

        var (merchantFound, offer) = await service.CreateAsync(
            NewCreateRequest(merchantId: MissingMerchantId),
            CancellationToken.None);

        Assert.That(merchantFound, Is.False);
        Assert.That(offer, Is.Null);

        await using var verifyContext = CreateContext();
        Assert.That(verifyContext.SpecialOffers.Count(), Is.EqualTo(0));
    }

    // ---------- ListAsync（US-07：商家查看满减活动）----------

    [Test]
    public async Task ListAsync_for_missing_merchant_returns_null()
    {
        await using var context = CreateContext();
        var service = new SpecialOfferService(context);

        var offers = await service.ListAsync(MissingMerchantId, CancellationToken.None);

        Assert.That(offers, Is.Null);
    }

    [Test]
    public async Task ListAsync_returns_empty_list_when_merchant_has_no_offers()
    {
        await using var context = CreateContext();
        var service = new SpecialOfferService(context);

        var offers = await service.ListAsync(MerchantId, CancellationToken.None);

        Assert.That(offers, Is.Not.Null);
        Assert.That(offers, Is.Empty);
    }

    [Test]
    public async Task ListAsync_returns_only_the_requested_merchants_offers_ordered_by_id()
    {
        await using (var seedContext = CreateContext())
        {
            var seedService = new SpecialOfferService(seedContext);
            await seedService.CreateAsync(NewCreateRequest(minPrice: 50.00m, amountRemission: 5.00m), CancellationToken.None);
            await seedService.CreateAsync(NewCreateRequest(minPrice: 80.00m, amountRemission: 8.00m), CancellationToken.None);
            await seedService.CreateAsync(
                NewCreateRequest(merchantId: OtherMerchantId, minPrice: 30.00m, amountRemission: 3.00m),
                CancellationToken.None);
        }

        await using var context = CreateContext();
        var service = new SpecialOfferService(context);

        var offers = await service.ListAsync(MerchantId, CancellationToken.None);

        Assert.That(offers, Is.Not.Null);
        Assert.That(offers, Has.Count.EqualTo(2));
        Assert.That(offers!.All(o => o.MerchantId == MerchantId), Is.True);
        Assert.That(offers![0].MinPrice, Is.EqualTo(50.00m));
        Assert.That(offers![1].MinPrice, Is.EqualTo(80.00m));
    }

    // ---------- UpdateAsync（US-06：商家编辑满减活动）----------

    [Test]
    public async Task UpdateAsync_changes_amounts_and_persists()
    {
        int offerId;
        await using (var seedContext = CreateContext())
        {
            var seedService = new SpecialOfferService(seedContext);
            var (_, created) = await seedService.CreateAsync(NewCreateRequest(), CancellationToken.None);
            offerId = created!.Id;
        }

        await using var context = CreateContext();
        var service = new SpecialOfferService(context);

        var updated = await service.UpdateAsync(
            offerId,
            MerchantId,
            new UpdateSpecialOfferRequest { MinPrice = 80.00m, AmountRemission = 8.00m },
            CancellationToken.None);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Id, Is.EqualTo(offerId));
        Assert.That(updated.MinPrice, Is.EqualTo(80.00m));
        Assert.That(updated.AmountRemission, Is.EqualTo(8.00m));

        await using var verifyContext = CreateContext();
        var persisted = verifyContext.SpecialOffers.Single(o => o.Id == offerId);
        Assert.That(persisted.MinPrice, Is.EqualTo(80.00m));
        Assert.That(persisted.AmountRemission, Is.EqualTo(8.00m));
    }

    [Test]
    public async Task UpdateAsync_for_another_merchant_returns_null_and_leaves_data_unchanged()
    {
        int offerId;
        await using (var seedContext = CreateContext())
        {
            var seedService = new SpecialOfferService(seedContext);
            var (_, created) = await seedService.CreateAsync(NewCreateRequest(), CancellationToken.None);
            offerId = created!.Id;
        }

        await using var context = CreateContext();
        var service = new SpecialOfferService(context);

        var updated = await service.UpdateAsync(
            offerId,
            OtherMerchantId,
            new UpdateSpecialOfferRequest { MinPrice = 999.00m, AmountRemission = 99.00m },
            CancellationToken.None);

        Assert.That(updated, Is.Null);

        await using var verifyContext = CreateContext();
        var persisted = verifyContext.SpecialOffers.Single(o => o.Id == offerId);
        Assert.That(persisted.MinPrice, Is.EqualTo(50.00m));
    }

    [Test]
    public async Task UpdateAsync_for_missing_offer_returns_null()
    {
        await using var context = CreateContext();
        var service = new SpecialOfferService(context);

        var updated = await service.UpdateAsync(
            999,
            MerchantId,
            new UpdateSpecialOfferRequest { MinPrice = 80.00m, AmountRemission = 8.00m },
            CancellationToken.None);

        Assert.That(updated, Is.Null);
    }

    // ---------- DeleteAsync（US-06：商家删除满减活动）----------

    [Test]
    public async Task DeleteAsync_removes_the_offer_and_returns_true()
    {
        int offerId;
        await using (var seedContext = CreateContext())
        {
            var seedService = new SpecialOfferService(seedContext);
            var (_, created) = await seedService.CreateAsync(NewCreateRequest(), CancellationToken.None);
            offerId = created!.Id;
        }

        await using var context = CreateContext();
        var service = new SpecialOfferService(context);

        var deleted = await service.DeleteAsync(offerId, MerchantId, CancellationToken.None);

        Assert.That(deleted, Is.True);

        await using var verifyContext = CreateContext();
        Assert.That(verifyContext.SpecialOffers.Any(o => o.Id == offerId), Is.False);
    }

    [Test]
    public async Task DeleteAsync_for_another_merchant_returns_false_and_keeps_the_offer()
    {
        int offerId;
        await using (var seedContext = CreateContext())
        {
            var seedService = new SpecialOfferService(seedContext);
            var (_, created) = await seedService.CreateAsync(NewCreateRequest(), CancellationToken.None);
            offerId = created!.Id;
        }

        await using var context = CreateContext();
        var service = new SpecialOfferService(context);

        var deleted = await service.DeleteAsync(offerId, OtherMerchantId, CancellationToken.None);

        Assert.That(deleted, Is.False);

        await using var verifyContext = CreateContext();
        Assert.That(verifyContext.SpecialOffers.Any(o => o.Id == offerId), Is.True);
    }

    [Test]
    public async Task DeleteAsync_for_missing_offer_returns_false()
    {
        await using var context = CreateContext();
        var service = new SpecialOfferService(context);

        var deleted = await service.DeleteAsync(999, MerchantId, CancellationToken.None);

        Assert.That(deleted, Is.False);
    }
}
