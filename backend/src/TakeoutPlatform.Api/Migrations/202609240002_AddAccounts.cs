using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TakeoutPlatform.Api.Data;

#nullable disable

namespace TakeoutPlatform.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609240002_AddAccounts")]
public partial class AddAccounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Accounts",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                NormalizedUsername = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ProfileId = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Accounts", x => x.Id));

        migrationBuilder.Sql("""
            INSERT INTO "Accounts" ("Username", "NormalizedUsername", "PasswordHash", "Role", "ProfileId", "CreatedAtUtc")
            SELECT "Username", UPPER("Username"), "PasswordHash", 'Merchant', "Id", NOW()
            FROM "Merchants"
            WHERE "Username" IS NOT NULL AND "PasswordHash" IS NOT NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_NormalizedUsername",
            table: "Accounts",
            column: "NormalizedUsername",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Accounts_Role_ProfileId",
            table: "Accounts",
            columns: new[] { "Role", "ProfileId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "Accounts");
}
