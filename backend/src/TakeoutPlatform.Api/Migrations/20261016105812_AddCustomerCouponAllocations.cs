using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TakeoutPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCouponAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerCoupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CouponId = table.Column<int>(type: "integer", nullable: false),
                    IssuedQuantity = table.Column<int>(type: "integer", nullable: false),
                    UsedQuantity = table.Column<int>(type: "integer", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCoupons", x => x.Id);
                    table.CheckConstraint("CK_CustomerCoupons_IssuedQuantity_Positive", "\"IssuedQuantity\" > 0");
                    table.CheckConstraint("CK_CustomerCoupons_UsedQuantity_Valid", "\"UsedQuantity\" >= 0 AND \"UsedQuantity\" <= \"IssuedQuantity\"");
                    table.ForeignKey(
                        name: "FK_CustomerCoupons_PlatformCoupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "PlatformCoupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerCoupons_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCoupons_CouponId",
                table: "CustomerCoupons",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCoupons_UserId_CouponId",
                table: "CustomerCoupons",
                columns: new[] { "UserId", "CouponId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerCoupons");
        }
    }
}
