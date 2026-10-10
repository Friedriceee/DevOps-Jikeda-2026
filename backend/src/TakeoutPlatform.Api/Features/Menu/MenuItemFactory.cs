using System.ComponentModel.DataAnnotations;
using TakeoutPlatform.Api.Features.Merchant;

namespace TakeoutPlatform.Api.Features.Menu;

public enum MenuItemType { Standard, Bundle, Promotional }

public sealed record MenuItemOptions(int? BundleSize = null,
    DateTimeOffset? PromotionStartsAtUtc = null, DateTimeOffset? PromotionEndsAtUtc = null);

public abstract record MenuItem(MenuItemType Type, MenuItemOptions Options)
{
    public abstract bool IsAvailableAt(DateTimeOffset now);
}

public sealed record StandardMenuItem() : MenuItem(MenuItemType.Standard, new())
{
    public override bool IsAvailableAt(DateTimeOffset now) => true;
}

// One inventory unit is one complete bundle, not one component of the bundle.
public sealed record BundleMenuItem(int Size) : MenuItem(MenuItemType.Bundle, new(Size))
{
    public override bool IsAvailableAt(DateTimeOffset now) => true;
}

public sealed record PromotionalMenuItem(DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc)
    : MenuItem(MenuItemType.Promotional, new(null, StartsAtUtc, EndsAtUtc))
{
    public override bool IsAvailableAt(DateTimeOffset now) => StartsAtUtc <= now && now < EndsAtUtc;
}

public abstract class MenuItemFactory
{
    public abstract MenuItemType Type { get; }
    public MenuItem Create(MenuItemOptions options) => CreateItem(options);
    protected abstract MenuItem CreateItem(MenuItemOptions options);

    public static MenuItemFactory For(string? type) => type?.ToLowerInvariant() switch
    {
        "standard" => new StandardMenuItemFactory(),
        "bundle" => new BundleMenuItemFactory(),
        "promotional" => new PromotionalMenuItemFactory(),
        _ => throw new ArgumentException("Item type must be Standard, Bundle, or Promotional."),
    };

    public static MenuItem FromDish(Dish dish) => For(dish.ItemType.ToString()).Create(
        new(dish.BundleSize, dish.PromotionStartsAtUtc, dish.PromotionEndsAtUtc));
}

public sealed class StandardMenuItemFactory : MenuItemFactory
{
    public override MenuItemType Type => MenuItemType.Standard;
    protected override MenuItem CreateItem(MenuItemOptions options)
    {
        if (options != new MenuItemOptions())
            throw new ArgumentException("Standard items cannot have bundle or promotion settings.");
        return new StandardMenuItem();
    }
}

public sealed class BundleMenuItemFactory : MenuItemFactory
{
    public override MenuItemType Type => MenuItemType.Bundle;
    protected override MenuItem CreateItem(MenuItemOptions options)
    {
        if (options.BundleSize is null or < 2)
            throw new ArgumentException("Bundle size must be a whole number of at least 2.");
        if (options.PromotionStartsAtUtc is not null || options.PromotionEndsAtUtc is not null)
            throw new ArgumentException("Bundle items cannot have promotion dates.");
        return new BundleMenuItem(options.BundleSize.Value);
    }
}

public sealed class PromotionalMenuItemFactory : MenuItemFactory
{
    public override MenuItemType Type => MenuItemType.Promotional;
    protected override MenuItem CreateItem(MenuItemOptions options)
    {
        if (options.BundleSize is not null)
            throw new ArgumentException("Promotional items cannot have a bundle size.");
        if (options.PromotionStartsAtUtc is null || options.PromotionEndsAtUtc is null)
            throw new ArgumentException("Promotional items require a start and end time.");
        if (options.PromotionEndsAtUtc <= options.PromotionStartsAtUtc)
            throw new ArgumentException("Promotion end time must be after its start time.");
        return new PromotionalMenuItem(options.PromotionStartsAtUtc.Value.ToUniversalTime(),
            options.PromotionEndsAtUtc.Value.ToUniversalTime());
    }
}

public interface IMenuItemRequest
{
    string ItemType { get; }
    int? BundleSize { get; }
    DateTimeOffset? PromotionStartsAtUtc { get; }
    DateTimeOffset? PromotionEndsAtUtc { get; }
}

public static class MenuItemValidation
{
    public static IEnumerable<ValidationResult> Validate(IMenuItemRequest request)
    {
        string? message = null;
        try { Create(request); }
        catch (ArgumentException error) { message = error.Message; }
        if (message is not null)
            yield return new ValidationResult(message, new[] { nameof(request.ItemType) });
    }

    public static MenuItem Create(IMenuItemRequest request) => MenuItemFactory.For(request.ItemType)
        .Create(new(request.BundleSize, request.PromotionStartsAtUtc, request.PromotionEndsAtUtc));
}
