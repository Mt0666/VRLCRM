using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VRLCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureDiscountRateColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Orders', 'DiscountAmount') IS NOT NULL
   AND COL_LENGTH('dbo.Orders', 'DiscountRate') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Orders.DiscountAmount', 'DiscountRate', 'COLUMN';
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Orders', 'DiscountRate') IS NOT NULL
   AND COL_LENGTH('dbo.Orders', 'DiscountAmount') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Orders.DiscountRate', 'DiscountAmount', 'COLUMN';
END");
        }
    }
}
