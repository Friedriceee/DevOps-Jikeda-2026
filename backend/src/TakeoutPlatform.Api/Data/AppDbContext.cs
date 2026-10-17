using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Features.Merchant;
using TakeoutPlatform.Api.Features.User;
using TakeoutPlatform.Api.Features.Auth;
using TakeoutPlatform.Api.Features.Cart;
using TakeoutPlatform.Api.Features.Pricing;
using TakeoutPlatform.Api.Features.Inventory;
using TakeoutPlatform.Api.Features.Checkout;
using Order = TakeoutPlatform.Api.Features.Order.Order;
using OrderUser = TakeoutPlatform.Api.Features.Order.OrderUser;
using OrderRider = TakeoutPlatform.Api.Features.Order.OrderRider;
using OrderDish = TakeoutPlatform.Api.Features.Order.OrderDish;
using OrderCoupon = TakeoutPlatform.Api.Features.Order.OrderCoupon;
using MerchantOrderFulfillment = TakeoutPlatform.Api.Features.Order.MerchantOrderFulfillment;
using Rider = TakeoutPlatform.Api.Features.Rider.Rider;
using RiderDelivery = TakeoutPlatform.Api.Features.Rider.RiderDelivery;
using AuditLog = TakeoutPlatform.Api.Features.Audit.AuditLog;

namespace TakeoutPlatform.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<SpecialOffer> SpecialOffers => Set<SpecialOffer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderUser> OrderUsers => Set<OrderUser>();
    public DbSet<OrderRider> OrderRiders => Set<OrderRider>();
    public DbSet<OrderDish> OrderDishes => Set<OrderDish>();
    public DbSet<OrderCoupon> OrderCoupons => Set<OrderCoupon>();
    public DbSet<MerchantOrderFulfillment> MerchantOrderFulfillments => Set<MerchantOrderFulfillment>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<PlatformCoupon> PlatformCoupons => Set<PlatformCoupon>();
    public DbSet<CustomerCoupon> CustomerCoupons => Set<CustomerCoupon>();
    public DbSet<Rider> Riders => Set<Rider>();
    public DbSet<RiderDelivery> RiderDeliveries => Set<RiderDelivery>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Actor).HasMaxLength(100).IsRequired();
            entity.Property(log => log.Role).HasMaxLength(30).IsRequired();
            entity.Property(log => log.Action).HasMaxLength(80).IsRequired();
            entity.Property(log => log.TargetType).HasMaxLength(80).IsRequired();
            entity.Property(log => log.TargetId).HasMaxLength(100).IsRequired();
            entity.Property(log => log.Result).HasMaxLength(200).IsRequired();
            entity.Property(log => log.CorrelationId).HasMaxLength(100).IsRequired();
            entity.HasIndex(log => log.TimestampUtc);
            entity.HasIndex(log => new { log.TargetType, log.TargetId });
            entity.HasIndex(log => log.CorrelationId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(payment => payment.IdempotencyKey).HasMaxLength(64).IsRequired();
            entity.Property(payment => payment.RequestHash).HasMaxLength(64).IsRequired();
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.Method).HasMaxLength(40).IsRequired();
            entity.Property(payment => payment.Status).HasConversion<int>();
            entity.Property(payment => payment.Reference).HasMaxLength(64).IsRequired();
            entity.Property(payment => payment.PricingSnapshotJson).IsRequired();
            entity.HasIndex(payment => payment.OrderId).IsUnique();
            entity.HasIndex(payment => new { payment.UserId, payment.IdempotencyKey }).IsUnique();
            entity.HasOne(payment => payment.Order)
                .WithMany()
                .HasForeignKey(payment => payment.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

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

            // A second deterministic staging merchant lets the checkout and
            // fulfilment flow be exercised with a genuine multi-restaurant
            // order.  The login for this profile is created by StagingSeeder
            // so its password still comes from the deployment secret.
            entity.HasData(new Merchant
            {
                Id = 2,
                Name = "Sprint 3 Merchant",
                Address = "Campus North Gate",
                Contact = "13800000002",
                DishType = "Noodles",
            });
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(account => account.Username).HasMaxLength(50).IsRequired();
            entity.Property(account => account.NormalizedUsername).HasMaxLength(50).IsRequired();
            entity.Property(account => account.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(account => account.Role).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(account => account.NormalizedUsername).IsUnique();
            entity.HasIndex(account => new { account.Role, account.ProfileId }).IsUnique();

            // Built-in administrator profile. It deliberately ships WITHOUT a usable password: a
            // credential committed to the repository would be a working login on every database
            // created from these migrations, production included. The password comes from
            // configuration (Admin__InitialPassword, or Seed__Password in staging); see AdminBootstrap.
            entity.HasData(new Account
            {
                Id = 1000,
                Username = "admin",
                NormalizedUsername = "ADMIN",
                PasswordHash = PasswordHashes.Unusable,
                Role = AccountRole.Administrator,
                ProfileId = 0,
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
            });
        });

        modelBuilder.Entity<Dish>(entity =>
        {
            entity.Property(dish => dish.ItemType).HasConversion<string>().HasMaxLength(20);
            entity.ToTable(table => table.HasCheckConstraint("CK_Dishes_Inventory_NonNegative", "\"Inventory\" >= 0"));
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

        modelBuilder.Entity<InventoryReservation>(entity =>
        {
            entity.HasKey(item => new { item.OrderId, item.DishId });
            entity.HasIndex(item => item.DishId);
            entity.HasIndex(item => item.MerchantId);
            entity.HasOne<Order>().WithMany().HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Dish>().WithMany().HasForeignKey(item => item.DishId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_InventoryReservations_Quantity_Positive", "\"Quantity\" > 0"));
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

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.Property(item => item.DishNum).IsRequired();
            entity.HasIndex(item => new { item.UserId, item.DishId }).IsUnique();
            entity.HasIndex(item => item.MerchantId);

            entity.HasOne(item => item.User)
                .WithMany(user => user.CartItems)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Dish)
                .WithMany()
                .HasForeignKey(item => item.DishId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlatformCoupon>(entity =>
        {
            entity.Property(coupon => coupon.Code).HasMaxLength(40).IsRequired();
            entity.Property(coupon => coupon.Name).HasMaxLength(100).IsRequired();
            entity.Property(coupon => coupon.MinimumSpend).HasPrecision(18, 2);
            entity.Property(coupon => coupon.DiscountAmount).HasPrecision(18, 2);
            entity.HasIndex(coupon => coupon.Code).IsUnique();

            entity.HasData(
                new PlatformCoupon
                {
                    Id = 1,
                    Code = "WELCOME5",
                    Name = "Welcome ¥5 off",
                    MinimumSpend = 20m,
                    DiscountAmount = 5m,
                    StartsAtUtc = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    ExpiresAtUtc = new DateTimeOffset(2099, 12, 31, 23, 59, 59, TimeSpan.Zero),
                    IsActive = true,
                },
                new PlatformCoupon
                {
                    Id = 2,
                    Code = "SAVE12",
                    Name = "Save ¥12 on larger orders",
                    MinimumSpend = 80m,
                    DiscountAmount = 12m,
                    StartsAtUtc = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    ExpiresAtUtc = new DateTimeOffset(2099, 12, 31, 23, 59, 59, TimeSpan.Zero),
                    IsActive = true,
                });
        });

        modelBuilder.Entity<CustomerCoupon>(entity =>
        {
            entity.HasKey(allocation => allocation.Id);
            entity.HasIndex(allocation => new { allocation.UserId, allocation.CouponId }).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_CustomerCoupons_IssuedQuantity_Positive", "\"IssuedQuantity\" > 0");
                table.HasCheckConstraint("CK_CustomerCoupons_UsedQuantity_Valid", "\"UsedQuantity\" >= 0 AND \"UsedQuantity\" <= \"IssuedQuantity\"");
            });
            entity.HasOne<Features.User.User>()
                .WithMany()
                .HasForeignKey(allocation => allocation.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(allocation => allocation.Coupon)
                .WithMany()
                .HasForeignKey(allocation => allocation.CouponId)
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

        modelBuilder.Entity<MerchantOrderFulfillment>(entity =>
        {
            entity.HasKey(item => new { item.OrderId, item.MerchantId });
            entity.Property(item => item.Status).HasConversion<int>();
            entity.Property(item => item.RejectionReason).HasMaxLength(500);
            entity.HasIndex(item => item.MerchantId);

            entity.HasOne(item => item.Order)
                .WithMany(order => order.MerchantFulfillments)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Merchant>()
                .WithMany()
                .HasForeignKey(item => item.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- 骑手与配送（Sprint 3 member5）----------
        modelBuilder.Entity<Rider>(entity =>
        {
            entity.HasKey(rider => rider.Id);
            entity.Property(rider => rider.Name).HasMaxLength(50).IsRequired();
            entity.Property(rider => rider.PhoneNumber).HasMaxLength(11);
            entity.Property(rider => rider.Zone).HasMaxLength(50).IsRequired();
            entity.HasIndex(rider => rider.Zone);

            // 预置演示骑手：同区在职可用、离职、下线、满载各一个，便于责任链演示与测试。
            entity.HasData(
                new Rider { Id = 1, Name = "Rider Zhang", PhoneNumber = "13700000001", IsActive = true, Zone = "ZoneA", IsAvailable = true, Capacity = 3, ActiveDeliveryCount = 0 },
                new Rider { Id = 2, Name = "Rider Li", PhoneNumber = "13700000002", IsActive = true, Zone = "ZoneA", IsAvailable = true, Capacity = 2, ActiveDeliveryCount = 0 },
                new Rider { Id = 3, Name = "Rider Off", PhoneNumber = "13700000003", IsActive = false, Zone = "ZoneA", IsAvailable = true, Capacity = 3, ActiveDeliveryCount = 0 });
        });

        modelBuilder.Entity<RiderDelivery>(entity =>
        {
            entity.HasKey(delivery => delivery.Id);
            entity.Property(delivery => delivery.Status).HasConversion<int>();
            // OrderId 唯一：一个订单只能有一条配送指派，防止重复接单。
            entity.HasIndex(delivery => delivery.OrderId).IsUnique();
            entity.HasIndex(delivery => delivery.RiderId);

            entity.HasOne(delivery => delivery.Rider)
                .WithMany(rider => rider.Deliveries)
                .HasForeignKey(delivery => delivery.RiderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(delivery => delivery.Order)
                .WithMany()
                .HasForeignKey(delivery => delivery.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceAuditAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceAuditAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnforceAuditAppendOnly()
    {
        var mutation = ChangeTracker.Entries<AuditLog>()
            .FirstOrDefault(entry => entry.State is EntityState.Modified or EntityState.Deleted);

        if (mutation is not null)
            throw new InvalidOperationException("Audit logs are append-only and cannot be changed or deleted.");
    }
}
