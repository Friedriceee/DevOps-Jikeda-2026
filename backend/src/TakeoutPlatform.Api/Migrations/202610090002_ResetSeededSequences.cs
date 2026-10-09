using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TakeoutPlatform.Api.Data;

#nullable disable

namespace TakeoutPlatform.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202610090002_ResetSeededSequences")]
public partial class ResetSeededSequences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            SELECT setval(
                pg_get_serial_sequence('"Merchants"', 'Id'),
                COALESCE((SELECT MAX("Id") FROM "Merchants"), 1),
                true);
            SELECT setval(
                pg_get_serial_sequence('"Users"', 'Id'),
                COALESCE((SELECT MAX("Id") FROM "Users"), 1),
                true);
            SELECT setval(
                pg_get_serial_sequence('"PlatformCoupons"', 'Id'),
                COALESCE((SELECT MAX("Id") FROM "PlatformCoupons"), 1),
                true);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Sequence values are operational state and should not be moved backwards.
    }
}
