using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TakeoutPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuFactoryAndInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BundleSize",
                table: "Dishes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "Dishes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Standard");
            migrationBuilder.Sql("ALTER TABLE \"Dishes\" ALTER COLUMN \"ItemType\" DROP DEFAULT;");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PromotionEndsAtUtc",
                table: "Dishes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PromotionStartsAtUtc",
                table: "Dishes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    DishId = table.Column<int>(type: "integer", nullable: false),
                    MerchantId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IsReleased = table.Column<bool>(type: "boolean", nullable: false),
                    ReservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleasedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservations", x => new { x.OrderId, x.DishId });
                    table.CheckConstraint("CK_InventoryReservations_Quantity_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "Dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Dishes_Inventory_NonNegative",
                table: "Dishes",
                sql: "\"Inventory\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_DishId",
                table: "InventoryReservations",
                column: "DishId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_MerchantId",
                table: "InventoryReservations",
                column: "MerchantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryReservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Dishes_Inventory_NonNegative",
                table: "Dishes");

            migrationBuilder.DropColumn(
                name: "BundleSize",
                table: "Dishes");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "Dishes");

            migrationBuilder.DropColumn(
                name: "PromotionEndsAtUtc",
                table: "Dishes");

            migrationBuilder.DropColumn(
                name: "PromotionStartsAtUtc",
                table: "Dishes");
        }
    }
}
