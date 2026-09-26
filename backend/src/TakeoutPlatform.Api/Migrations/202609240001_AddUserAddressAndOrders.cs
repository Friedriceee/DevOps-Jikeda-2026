using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TakeoutPlatform.Api.Data;

#nullable disable

namespace TakeoutPlatform.Api.Migrations;

/// <summary>
/// 新增用户、用户地址、以及订单五表结构（Orders/OrderUsers/OrderRiders/OrderDishes/OrderCoupons）。
/// 本迁移为手写，与仓库现有迁移风格一致（PostgreSQL 类型 + 手动命名）。
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("202609240001_AddUserAddressAndOrders")]
public partial class AddUserAddressAndOrders : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ---------- Users ----------
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                PhoneNumber = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                Wallet = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        // ---------- UserAddresses ----------
        migrationBuilder.CreateTable(
            name: "UserAddresses",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: false),
                Address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                HouseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                ContactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PhoneNumber = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserAddresses", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserAddresses_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ---------- Orders ----------
        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                OrderTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExpectedTimeOfArrival = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RealTimeOfArrival = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                NeedUtensils = table.Column<int>(type: "integer", nullable: false),
                AddressId = table.Column<int>(type: "integer", nullable: false),
                MerchantRating = table.Column<int>(type: "integer", nullable: true),
                RiderRating = table.Column<int>(type: "integer", nullable: true),
                Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
            });

        // ---------- OrderUsers ----------
        migrationBuilder.CreateTable(
            name: "OrderUsers",
            columns: table => new
            {
                OrderId = table.Column<int>(type: "integer", nullable: false),
                UserId = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderUsers", x => x.OrderId);
                table.ForeignKey(
                    name: "FK_OrderUsers_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ---------- OrderRiders ----------
        migrationBuilder.CreateTable(
            name: "OrderRiders",
            columns: table => new
            {
                OrderId = table.Column<int>(type: "integer", nullable: false),
                RiderId = table.Column<int>(type: "integer", nullable: true),
                RiderPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderRiders", x => x.OrderId);
                table.ForeignKey(
                    name: "FK_OrderRiders_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ---------- OrderDishes ----------
        migrationBuilder.CreateTable(
            name: "OrderDishes",
            columns: table => new
            {
                OrderId = table.Column<int>(type: "integer", nullable: false),
                MerchantId = table.Column<int>(type: "integer", nullable: false),
                DishId = table.Column<int>(type: "integer", nullable: false),
                DishNum = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderDishes", x => new { x.OrderId, x.MerchantId, x.DishId });
                table.ForeignKey(
                    name: "FK_OrderDishes_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ---------- OrderCoupons ----------
        migrationBuilder.CreateTable(
            name: "OrderCoupons",
            columns: table => new
            {
                OrderId = table.Column<int>(type: "integer", nullable: false),
                UserId = table.Column<int>(type: "integer", nullable: false),
                CouponId = table.Column<int>(type: "integer", nullable: false),
                ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderCoupons", x => x.OrderId);
                table.ForeignKey(
                    name: "FK_OrderCoupons_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ---------- 种子数据：演示用户 ----------
        migrationBuilder.InsertData(
            table: "Users",
            columns: new[] { "Id", "PhoneNumber", "UserName", "Wallet" },
            columnTypes: new[] { "integer", "character varying(11)", "character varying(50)", "numeric(18,2)" },
            values: new object[] { 1, "13800000000", "Demo User", 1000.00m });

        // ---------- 索引 ----------
        migrationBuilder.CreateIndex("IX_Orders_AddressId", "Orders", "AddressId");
        migrationBuilder.CreateIndex("IX_OrderUsers_UserId", "OrderUsers", "UserId");
        migrationBuilder.CreateIndex("IX_UserAddresses_UserId", "UserAddresses", "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("OrderCoupons");
        migrationBuilder.DropTable("OrderDishes");
        migrationBuilder.DropTable("OrderRiders");
        migrationBuilder.DropTable("OrderUsers");
        migrationBuilder.DropTable("Orders");
        migrationBuilder.DropTable("UserAddresses");
        migrationBuilder.DropTable("Users");
    }
}
