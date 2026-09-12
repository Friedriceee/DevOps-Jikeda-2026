using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Features.Merchant;

namespace TakeoutPlatform.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<SpecialOffer> SpecialOffers => Set<SpecialOffer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.Property(merchant => merchant.Name)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(merchant => merchant.Username).HasMaxLength(50);
            entity.HasIndex(merchant => merchant.Username).IsUnique();
            entity.Property(merchant => merchant.PasswordHash).HasMaxLength(500);
            entity.Property(merchant => merchant.Address).HasMaxLength(255);
            entity.Property(merchant => merchant.Contact).HasMaxLength(255);
            entity.Property(merchant => merchant.DishType).HasMaxLength(20);
            entity.Property(merchant => merchant.WalletPasswordHash).HasMaxLength(500);
            entity.Property(merchant => merchant.Wallet).HasPrecision(18, 2);

            entity.HasData(new Merchant
            {
                Id = 1,
                Name = "Demo Merchant",
            });
        });

        modelBuilder.Entity<Dish>(entity =>
        {
            entity.Property(dish => dish.Name)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(dish => dish.Price)
                .HasPrecision(18, 2);
            entity.Property(dish => dish.Category)
                .HasMaxLength(20);
            entity.Property(dish => dish.ImageUrl)
                .HasMaxLength(500);

            entity.HasOne(dish => dish.Merchant)
                .WithMany(merchant => merchant.Dishes)
                .HasForeignKey(dish => dish.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SpecialOffer>(entity =>
        {
            entity.Property(offer => offer.MinPrice)
                .HasPrecision(18, 2);
            entity.Property(offer => offer.AmountRemission)
                .HasPrecision(18, 2);

            entity.HasIndex(offer => offer.MerchantId);

            entity.HasOne(offer => offer.Merchant)
                .WithMany(merchant => merchant.SpecialOffers)
                .HasForeignKey(offer => offer.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
