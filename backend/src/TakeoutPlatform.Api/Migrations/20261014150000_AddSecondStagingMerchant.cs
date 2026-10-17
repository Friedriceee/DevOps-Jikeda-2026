using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TakeoutPlatform.Api.Data;

#nullable disable

namespace TakeoutPlatform.Api.Migrations;

/// <summary>
/// Adds the deterministic second merchant used by the staging multi-restaurant
/// checkout demonstration.  The login and menu inventory remain idempotent
/// runtime seed data; this migration only guarantees the referenced merchant
/// profile exists before those rows are created.
///
/// Without the [DbContext]/[Migration] attributes EF Core does not discover this
/// class, so it never ran: a freshly created database had no merchant 2 and the
/// staging seeder failed with a foreign-key violation on start-up.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261014150000_AddSecondStagingMerchant")]
public partial class AddSecondStagingMerchant : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Use a guarded insert because some Sprint 3 staging databases may
        // already contain a merchant with id 2 from an earlier manual seed.
        migrationBuilder.Sql(
            """
            INSERT INTO "Merchants" ("Id", "Address", "Contact", "CouponType", "DishType", "Name", "Wallet")
            SELECT 2, 'Campus North Gate', '13800000002', 0, 'Noodles', 'Sprint 3 Merchant', 0
            WHERE NOT EXISTS (SELECT 1 FROM "Merchants" WHERE "Id" = 2);
            """);

        // The seeded id is intentional so existing multi-restaurant orders can
        // be fulfilled by the deterministic sprint3merchant account. Keep the
        // PostgreSQL identity sequence above the inserted key for later signups.
        migrationBuilder.Sql(
            """
            SELECT setval(
                pg_get_serial_sequence('"Merchants"', 'Id'),
                COALESCE((SELECT MAX("Id") FROM "Merchants"), 1),
                true);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DELETE FROM \"Merchants\" WHERE \"Id\" = 2 AND \"Name\" = 'Sprint 3 Merchant';");
    }
}
