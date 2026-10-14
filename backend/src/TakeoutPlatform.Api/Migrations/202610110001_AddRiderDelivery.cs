using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TakeoutPlatform.Api.Data;

#nullable disable

namespace TakeoutPlatform.Api.Migrations;

/// <summary>
/// 新增骑手与配送指派表（Sprint 3 member5 — Rider Chain / Delivery）。
/// Riders 承载责任链筛选资格数据；RiderDeliveries 记录一次配送指派，
/// OrderId 唯一约束保证一个订单只能被一名骑手接走。
/// 手写迁移，风格与仓库现有迁移一致（PostgreSQL 类型 + 手动命名）。
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("202610110001_AddRiderDelivery")]
public partial class AddRiderDelivery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Riders",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                PhoneNumber = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                Zone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                Capacity = table.Column<int>(type: "integer", nullable: false),
                ActiveDeliveryCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Riders", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RiderDeliveries",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                OrderId = table.Column<int>(type: "integer", nullable: false),
                RiderId = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PickedUpAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeliveringAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RiderDeliveries", x => x.Id);
                table.ForeignKey(
                    name: "FK_RiderDeliveries_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_RiderDeliveries_Riders_RiderId",
                    column: x => x.RiderId,
                    principalTable: "Riders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // 种子骑手（与 AppDbContext.HasData 一致）
        migrationBuilder.InsertData(
            table: "Riders",
            columns: new[] { "Id", "Name", "PhoneNumber", "IsActive", "Zone", "IsAvailable", "Capacity", "ActiveDeliveryCount" },
            columnTypes: new[] { "integer", "character varying(50)", "character varying(11)", "boolean", "character varying(50)", "boolean", "integer", "integer" },
            values: new object[,]
            {
                { 1, "Rider Zhang", "13700000001", true, "ZoneA", true, 3, 0 },
                { 2, "Rider Li", "13700000002", true, "ZoneA", true, 2, 0 },
                { 3, "Rider Off", "13700000003", false, "ZoneA", true, 3, 0 },
            });

        migrationBuilder.CreateIndex("IX_Riders_Zone", "Riders", "Zone");
        migrationBuilder.CreateIndex("IX_RiderDeliveries_OrderId", "RiderDeliveries", "OrderId", unique: true);
        migrationBuilder.CreateIndex("IX_RiderDeliveries_RiderId", "RiderDeliveries", "RiderId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("RiderDeliveries");
        migrationBuilder.DropTable("Riders");
    }
}
