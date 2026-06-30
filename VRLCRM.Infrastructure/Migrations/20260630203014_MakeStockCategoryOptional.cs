using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VRLCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeStockCategoryOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockItems_Categories_CategoryId",
                schema: "dbo",
                table: "StockItems");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                schema: "dbo",
                table: "StockItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_StockItems_Categories_CategoryId",
                schema: "dbo",
                table: "StockItems",
                column: "CategoryId",
                principalSchema: "dbo",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockItems_Categories_CategoryId",
                schema: "dbo",
                table: "StockItems");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                schema: "dbo",
                table: "StockItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StockItems_Categories_CategoryId",
                schema: "dbo",
                table: "StockItems",
                column: "CategoryId",
                principalSchema: "dbo",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
