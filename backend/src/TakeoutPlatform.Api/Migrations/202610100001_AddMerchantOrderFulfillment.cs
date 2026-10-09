using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TakeoutPlatform.Api.Data;

#nullable disable

namespace TakeoutPlatform.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202610100001_AddMerchantOrderFulfillment")]
public partial class AddMerchantOrderFulfillment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MerchantOrderFulfillments",
            columns: table => new
            {
                OrderId = table.Column<int>(type: "integer", nullable: false),
                MerchantId = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                InventoryRestored = table.Column<bool>(type: "boolean", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MerchantOrderFulfillments", x => new { x.OrderId, x.MerchantId });
                table.ForeignKey(
                    name: "FK_MerchantOrderFulfillments_Merchants_MerchantId",
                    column: x => x.MerchantId,
                    principalTable: "Merchants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_MerchantOrderFulfillments_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MerchantOrderFulfillments_MerchantId",
            table: "MerchantOrderFulfillments",
            column: "MerchantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "MerchantOrderFulfillments");
}
