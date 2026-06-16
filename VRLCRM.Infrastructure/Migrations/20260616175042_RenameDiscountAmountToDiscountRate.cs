using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VRLCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameDiscountAmountToDiscountRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                schema: "dbo",
                table: "Orders",
                newName: "DiscountRate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiscountRate",
                schema: "dbo",
                table: "Orders",
                newName: "DiscountAmount");
        }
    }
}
