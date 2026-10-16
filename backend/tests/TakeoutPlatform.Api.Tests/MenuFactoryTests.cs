using System.ComponentModel.DataAnnotations;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Menu;
using TakeoutPlatform.Api.Features.Merchant;

namespace TakeoutPlatform.Api.Tests;

public class MenuFactoryTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-10-01T00:00:00Z");

    [TestCase("Standard", typeof(StandardMenuItem))]
    [TestCase("bundle", typeof(BundleMenuItem))]
    [TestCase("Promotional", typeof(PromotionalMenuItem))]
    public void Concrete_creators_produce_the_requested_product(string type, Type expected)
    {
        var options = type.ToLowerInvariant() switch
        {
            "bundle" => new MenuItemOptions(3),
            "promotional" => new MenuItemOptions(null, Start, Start.AddDays(1)),
            _ => new MenuItemOptions(),
        };
        Assert.That(MenuItemFactory.For(type).Create(options), Is.TypeOf(expected));
    }

    [TestCase("Unknown", null)]
    [TestCase("Standard", 2)]
    [TestCase("Bundle", null)]
    [TestCase("Bundle", 1)]
    [TestCase("Promotional", 2)]
    public void Invalid_type_or_type_specific_settings_are_rejected(string type, int? size) =>
        Assert.Throws<ArgumentException>((Action)(() => MenuItemFactory.For(type).Create(new(size))));

    [Test]
    public void Bundle_rejects_promotion_dates() => Assert.Throws<ArgumentException>((Action)(() =>
        new BundleMenuItemFactory().Create(new(2, Start, Start.AddDays(1)))));

    [Test]
    public void Promotion_rejects_non_increasing_dates() => Assert.Throws<ArgumentException>((Action)(() =>
        new PromotionalMenuItemFactory().Create(new(null, Start, Start))));

    [Test]
    public void Promotion_is_available_at_start_but_not_at_end_and_normalizes_UTC()
    {
        var item = new PromotionalMenuItemFactory().Create(new(null,
            Start.ToOffset(TimeSpan.FromHours(8)), Start.AddHours(1).ToOffset(TimeSpan.FromHours(8))));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(item.IsAvailableAt(Start.AddTicks(-1)), Is.False);
            Assert.That(item.IsAvailableAt(Start), Is.True);
            Assert.That(item.IsAvailableAt(Start.AddMinutes(30)), Is.True);
            Assert.That(item.IsAvailableAt(Start.AddHours(1)), Is.False);
            Assert.That(item.Options.PromotionStartsAtUtc!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
        }));
    }

    [Test]
    public void Create_and_update_requests_validate_factory_settings()
    {
        foreach (var request in new IValidatableObject[]
        {
            new CreateDishRequest { Name = "Meal", Price = 10, ItemType = "Bundle", BundleSize = 1 },
            new UpdateDishRequest { Name = "Meal", Price = 10, ItemType = "Promotional" },
        })
        {
            var results = new List<ValidationResult>();
            Assert.That(Validator.TryValidateObject(request, new(request), results, true), Is.False);
            Assert.That(results.Any(result => result.MemberNames.Contains("ItemType")), Is.True);
        }
    }
}

internal sealed class MenuTestClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
