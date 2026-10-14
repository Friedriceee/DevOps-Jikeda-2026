using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TakeoutPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountIsActiveAndSeedAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 现有账号默认保持启用（defaultValue: true），避免迁移后无法登录。
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "NormalizedUsername", "PasswordHash", "ProfileId", "Role", "Username" },
                values: new object[] { 1000, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "ADMIN", "AQAAAAIAAYagAAAAEP8/kHbqZOLjY1r3lOeyHus9BtbVjlwlQHFjJ4AvAWRSQwPvAllNkaLJ9XNK+nk6kA==", 0, "Administrator", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 1000);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Accounts");
        }
    }
}
