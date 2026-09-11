using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TakeoutPlatform.Api.Data;

#nullable disable

namespace TakeoutPlatform.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609110001_CreateMerchantAndDish")]
public partial class CreateMerchantAndDish : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Merchants",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Merchants", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Dishes",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MerchantId = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Inventory = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Dishes", x => x.Id);
                table.ForeignKey(
                    name: "FK_Dishes_Merchants_MerchantId",
                    column: x => x.MerchantId,
                    principalTable: "Merchants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Dishes_MerchantId",
            table: "Dishes",
            column: "MerchantId");

        migrationBuilder.InsertData(
            table: "Merchants",
            columns: new[] { "Id", "Name" },
            values: new object[] { 1, "Demo Merchant" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Dishes");
        migrationBuilder.DropTable(name: "Merchants");
    }
}
