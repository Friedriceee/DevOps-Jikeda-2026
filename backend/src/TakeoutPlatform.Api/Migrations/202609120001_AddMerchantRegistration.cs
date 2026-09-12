using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TakeoutPlatform.Api.Data;

#nullable disable

namespace TakeoutPlatform.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609120001_AddMerchantRegistration")]
public partial class AddMerchantRegistration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("Username", "Merchants", "character varying(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<string>("PasswordHash", "Merchants", "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>("Address", "Merchants", "character varying(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<string>("Contact", "Merchants", "character varying(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<string>("DishType", "Merchants", "character varying(20)", maxLength: 20, nullable: true);
        migrationBuilder.AddColumn<int>("TimeForOpenBusiness", "Merchants", "integer", nullable: true);
        migrationBuilder.AddColumn<int>("TimeForCloseBusiness", "Merchants", "integer", nullable: true);
        migrationBuilder.AddColumn<int>("CouponType", "Merchants", "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<decimal>("Wallet", "Merchants", "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<string>("WalletPasswordHash", "Merchants", "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.CreateIndex("IX_Merchants_Username", "Merchants", "Username", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_Merchants_Username", "Merchants");
        migrationBuilder.DropColumn("Username", "Merchants");
        migrationBuilder.DropColumn("PasswordHash", "Merchants");
        migrationBuilder.DropColumn("Address", "Merchants");
        migrationBuilder.DropColumn("Contact", "Merchants");
        migrationBuilder.DropColumn("DishType", "Merchants");
        migrationBuilder.DropColumn("TimeForOpenBusiness", "Merchants");
        migrationBuilder.DropColumn("TimeForCloseBusiness", "Merchants");
        migrationBuilder.DropColumn("CouponType", "Merchants");
        migrationBuilder.DropColumn("Wallet", "Merchants");
        migrationBuilder.DropColumn("WalletPasswordHash", "Merchants");
    }
}
