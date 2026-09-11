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
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.HasKey("Id");
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
    }
}
