using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TakeoutPlatform.Api.Data;

#nullable disable

namespace TakeoutPlatform.Api.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Auth.Account", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("NormalizedUsername").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<string>("PasswordHash").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
            b.Property<int>("ProfileId").HasColumnType("integer");
            b.Property<string>("Role").IsRequired().HasMaxLength(20).HasColumnType("character varying(20)");
            b.Property<string>("Username").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
            b.HasKey("Id");
            b.HasIndex("NormalizedUsername").IsUnique();
            b.HasIndex("Role", "ProfileId").IsUnique();
            b.ToTable("Accounts");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Merchant.Dish", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            b.Property<string>("Category")
                .HasMaxLength(20)
                .HasColumnType("character varying(20)");

            b.Property<string>("ImageUrl")
                .HasMaxLength(500)
                .HasColumnType("character varying(500)");

            b.Property<int>("Inventory")
                .HasColumnType("integer");

            b.Property<int>("MerchantId")
                .HasColumnType("integer");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("character varying(50)");

            b.Property<decimal>("Price")
                .HasPrecision(18, 2)
                .HasColumnType("numeric(18,2)");

            b.HasKey("Id");
            b.HasIndex("MerchantId");
            b.ToTable("Dishes");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Merchant.Merchant", b =>
        {
            b.Property<string>("Address").HasMaxLength(255).HasColumnType("character varying(255)");
            b.Property<string>("Contact").HasMaxLength(255).HasColumnType("character varying(255)");
            b.Property<int>("CouponType").HasColumnType("integer");
            b.Property<string>("DishType").HasMaxLength(20).HasColumnType("character varying(20)");
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            b.Property<string>("PasswordHash").HasMaxLength(500).HasColumnType("character varying(500)");
            b.Property<int?>("TimeForCloseBusiness").HasColumnType("integer");
            b.Property<int?>("TimeForOpenBusiness").HasColumnType("integer");
            b.Property<string>("Username").HasMaxLength(50).HasColumnType("character varying(50)");
            b.Property<decimal>("Wallet").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<string>("WalletPasswordHash").HasMaxLength(500).HasColumnType("character varying(500)");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.HasKey("Id");
            b.HasIndex("Username").IsUnique();
            b.ToTable("Merchants");
            b.HasData(new { Id = 1, Name = "Demo Merchant" });
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Merchant.SpecialOffer", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            b.Property<decimal>("AmountRemission")
                .HasPrecision(18, 2)
                .HasColumnType("numeric(18,2)");

            b.Property<int>("MerchantId")
                .HasColumnType("integer");

            b.Property<decimal>("MinPrice")
                .HasPrecision(18, 2)
                .HasColumnType("numeric(18,2)");

            b.HasKey("Id");
            b.HasIndex("MerchantId");
            b.ToTable("SpecialOffers");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.User.User", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            b.Property<string>("PhoneNumber").HasMaxLength(11).HasColumnType("character varying(11)");

            b.Property<string>("UserName")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("character varying(50)");

            b.Property<decimal>("Wallet").HasPrecision(18, 2).HasColumnType("numeric(18,2)");

            b.HasKey("Id");
            b.ToTable("Users");
            b.HasData(new { Id = 1, PhoneNumber = "13800000000", UserName = "Demo User", Wallet = 1000.00m });
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.User.UserAddress", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            b.Property<string>("Address")
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnType("character varying(255)");

            b.Property<string>("ContactName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<string>("HouseNumber").HasMaxLength(50).HasColumnType("character varying(50)");

            b.Property<string>("PhoneNumber")
                .IsRequired()
                .HasMaxLength(11)
                .HasColumnType("character varying(11)");

            b.Property<int>("UserId").HasColumnType("integer");

            b.HasKey("Id");
            b.HasIndex("UserId");
            b.ToTable("UserAddresses");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Order.Order", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            b.Property<int>("AddressId").HasColumnType("integer");
            b.Property<string>("Comment").HasMaxLength(500).HasColumnType("character varying(500)");
            b.Property<DateTime?>("ExpectedTimeOfArrival").HasColumnType("timestamp with time zone");
            b.Property<int>("MerchantRating").HasColumnType("integer");
            b.Property<int>("NeedUtensils").HasColumnType("integer");
            b.Property<DateTime>("OrderTimestamp").HasColumnType("timestamp with time zone");
            b.Property<decimal>("Price").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<DateTime?>("RealTimeOfArrival").HasColumnType("timestamp with time zone");
            b.Property<int>("RiderRating").HasColumnType("integer");
            b.Property<int>("Status").HasColumnType("integer");

            b.HasKey("Id");
            b.HasIndex("AddressId");
            b.ToTable("Orders");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Order.OrderUser", b =>
        {
            b.Property<int>("OrderId").HasColumnType("integer");
            b.Property<int>("UserId").HasColumnType("integer");

            b.HasKey("OrderId");
            b.HasIndex("UserId");
            b.ToTable("OrderUsers");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Order.OrderRider", b =>
        {
            b.Property<int>("OrderId").HasColumnType("integer");
            b.Property<int?>("RiderId").HasColumnType("integer");
            b.Property<decimal>("RiderPrice").HasPrecision(18, 2).HasColumnType("numeric(18,2)");

            b.HasKey("OrderId");
            b.ToTable("OrderRiders");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Order.OrderDish", b =>
        {
            b.Property<int>("OrderId").HasColumnType("integer");
            b.Property<int>("MerchantId").HasColumnType("integer");
            b.Property<int>("DishId").HasColumnType("integer");
            b.Property<int>("DishNum").HasColumnType("integer");

            b.HasKey("OrderId", "MerchantId", "DishId");
            b.ToTable("OrderDishes");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Order.OrderCoupon", b =>
        {
            b.Property<int>("OrderId").HasColumnType("integer");
            b.Property<int>("CouponId").HasColumnType("integer");
            b.Property<DateTime>("ExpirationDate").HasColumnType("timestamp with time zone");
            b.Property<int>("UserId").HasColumnType("integer");

            b.HasKey("OrderId");
            b.ToTable("OrderCoupons");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Merchant.Dish", b =>
        {
            b.HasOne("TakeoutPlatform.Api.Features.Merchant.Merchant", "Merchant")
                .WithMany("Dishes")
                .HasForeignKey("MerchantId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Merchant");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Merchant.Merchant", b =>
        {
            b.Navigation("Dishes");
            b.Navigation("SpecialOffers");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Merchant.SpecialOffer", b =>
        {
            b.HasOne("TakeoutPlatform.Api.Features.Merchant.Merchant", "Merchant")
                .WithMany("SpecialOffers")
                .HasForeignKey("MerchantId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Merchant");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.User.UserAddress", b =>
        {
            b.HasOne("TakeoutPlatform.Api.Features.User.User", "User")
                .WithMany("Addresses")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("User");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Order.OrderUser", b =>
        {
            b.HasOne("TakeoutPlatform.Api.Features.Order.Order", "Order")
                .WithOne("OrderUser")
                .HasForeignKey("TakeoutPlatform.Api.Features.Order.OrderUser", "OrderId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Order");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Order.OrderRider", b =>
        {
            b.HasOne("TakeoutPlatform.Api.Features.Order.Order", "Order")
                .WithOne("OrderRider")
                .HasForeignKey("TakeoutPlatform.Api.Features.Order.OrderRider", "OrderId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Order");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Order.OrderDish", b =>
        {
            b.HasOne("TakeoutPlatform.Api.Features.Order.Order", "Order")
                .WithMany("OrderDishes")
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Order");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Order.OrderCoupon", b =>
        {
            b.HasOne("TakeoutPlatform.Api.Features.Order.Order", "Order")
                .WithOne("OrderCoupon")
                .HasForeignKey("TakeoutPlatform.Api.Features.Order.OrderCoupon", "OrderId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Order");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.Order.Order", b =>
        {
            b.Navigation("OrderCoupon");
            b.Navigation("OrderDishes");
            b.Navigation("OrderRider");
            b.Navigation("OrderUser");
        });

        modelBuilder.Entity("TakeoutPlatform.Api.Features.User.User", b =>
        {
            b.Navigation("Addresses");
        });
    }
}
