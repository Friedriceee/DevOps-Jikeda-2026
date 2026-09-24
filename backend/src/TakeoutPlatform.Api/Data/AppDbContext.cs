using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.User;
using Order = TakeoutPlatform.Api.Features.Order.Order;
using OrderUser = TakeoutPlatform.Api.Features.Order.OrderUser;
using OrderRider = TakeoutPlatform.Api.Features.Order.OrderRider;
using OrderDish = TakeoutPlatform.Api.Features.Order.OrderDish;
using OrderCoupon = TakeoutPlatform.Api.Features.Order.OrderCoupon;

namespace TakeoutPlatform.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<SpecialOffer> SpecialOffers => Set<SpecialOffer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderUser> OrderUsers => Set<OrderUser>();
    public DbSet<OrderRider> OrderRiders => Set<OrderRider>();
    public DbSet<OrderDish> OrderDishes => Set<OrderDish>();
    public DbSet<OrderCoupon> OrderCoupons => Set<OrderCoupon>();

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

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(user => user.UserName)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(user => user.PhoneNumber).HasMaxLength(11);
            entity.Property(user => user.Wallet).HasPrecision(18, 2);

            // 预置一个演示用户，方便测试与本地联调。
            entity.HasData(new User
            {
                Id = 1,
                UserName = "Demo User",
                PhoneNumber = "13800000000",
                Wallet = 1000.00m,
            });
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.Property(address => address.Address)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(address => address.HouseNumber).HasMaxLength(50);
            entity.Property(address => address.ContactName)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(address => address.PhoneNumber)
                .HasMaxLength(11)
                .IsRequired();

            entity.HasIndex(address => address.UserId);

            entity.HasOne(address => address.User)
                .WithMany(user => user.Addresses)
                .HasForeignKey(address => address.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- 订单五表结构（对应旧项目 OrderDB 等）----------
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Price).HasPrecision(18, 2);
            // 枚举以 int 存储，与旧项目 State 数值兼容。
            entity.Property(order => order.Status).HasConversion<int>();
            entity.Property(order => order.Comment).HasMaxLength(500);
            entity.HasIndex(order => order.AddressId);
        });

        modelBuilder.Entity<OrderUser>(entity =>
        {
            entity.HasKey(orderUser => orderUser.OrderId);
            entity.HasIndex(orderUser => orderUser.UserId);

            entity.HasOne(orderUser => orderUser.Order)
                .WithOne(order => order.OrderUser)
                .HasForeignKey<OrderUser>(orderUser => orderUser.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderRider>(entity =>
        {
            entity.HasKey(orderRider => orderRider.OrderId);
            entity.Property(orderRider => orderRider.RiderPrice).HasPrecision(18, 2);

            entity.HasOne(orderRider => orderRider.Order)
                .WithOne(order => order.OrderRider)
                .HasForeignKey<OrderRider>(orderRider => orderRider.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderDish>(entity =>
        {
            entity.HasKey(orderDish => new { orderDish.OrderId, orderDish.MerchantId, orderDish.DishId });

            entity.HasOne(orderDish => orderDish.Order)
                .WithMany(order => order.OrderDishes)
                .HasForeignKey(orderDish => orderDish.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderCoupon>(entity =>
        {
            entity.HasKey(orderCoupon => orderCoupon.OrderId);

            entity.HasOne(orderCoupon => orderCoupon.Order)
                .WithOne(order => order.OrderCoupon)
                .HasForeignKey<OrderCoupon>(orderCoupon => orderCoupon.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
