using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TakeoutPlatform.Api.Data;

#nullable disable

namespace TakeoutPlatform.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202610090001_AddPlatformCoupons")]
public partial class AddPlatformCoupons : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PlatformCoupons",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                MinimumSpend = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlatformCoupons", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "PlatformCoupons",
            columns: new[] { "Id", "Code", "DiscountAmount", "ExpiresAtUtc", "IsActive", "MinimumSpend", "Name", "StartsAtUtc" },
            columnTypes: new[] { "integer", "character varying(40)", "numeric(18,2)", "timestamp with time zone", "boolean", "numeric(18,2)", "character varying(100)", "timestamp with time zone" },
            values: new object[,]
            {
                { 1, "WELCOME5", 5m, new DateTimeOffset(2099, 12, 31, 23, 59, 59, TimeSpan.Zero), true, 20m, "Welcome ¥5 off", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                { 2, "SAVE12", 12m, new DateTimeOffset(2099, 12, 31, 23, 59, 59, TimeSpan.Zero), true, 80m, "Save ¥12 on larger orders", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            });

        migrationBuilder.CreateIndex(
            name: "IX_PlatformCoupons_Code",
            table: "PlatformCoupons",
            column: "Code",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "PlatformCoupons");
}
