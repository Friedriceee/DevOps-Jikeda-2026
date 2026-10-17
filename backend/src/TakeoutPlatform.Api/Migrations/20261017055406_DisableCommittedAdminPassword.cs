using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TakeoutPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class DisableCommittedAdminPassword : Migration
    {
        /// <summary>
        /// The first administrator migration shipped a password hash for the publicly documented
        /// password "Admin@12345", which made every database created from the migrations
        /// (production included) log in as an administrator. Replace exactly that hash with an
        /// unusable one. A password someone has since chosen is left alone, which is why this is a
        /// guarded UPDATE rather than an unconditional UpdateData.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Accounts"
                SET "PasswordHash" = 'Ag=='
                WHERE "Id" = 1000
                  AND "PasswordHash" = 'AQAAAAIAAYagAAAAEP8/kHbqZOLjY1r3lOeyHus9BtbVjlwlQHFjJ4AvAWRSQwPvAllNkaLJ9XNK+nk6kA==';
                """);
        }

        /// <summary>Deliberately does not restore the committed credential.</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
