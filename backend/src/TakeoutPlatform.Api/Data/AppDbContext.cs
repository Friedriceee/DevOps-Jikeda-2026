using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Features.Merchant;

namespace TakeoutPlatform.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Dish> Dishes => Set<Dish>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.Property(merchant => merchant.Name)
                .HasMaxLength(100)
                .IsRequired();

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
    }
}
