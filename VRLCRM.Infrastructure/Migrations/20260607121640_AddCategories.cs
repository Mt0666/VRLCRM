using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VRLCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "dbo",
                table: "StockItems");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                schema: "dbo",
                table: "StockItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_CategoryId",
                schema: "dbo",
                table: "StockItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                schema: "dbo",
                table: "Categories",
                column: "Name",
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockItems_Categories_CategoryId",
                schema: "dbo",
                table: "StockItems");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_StockItems_CategoryId",
                schema: "dbo",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "dbo",
                table: "StockItems");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "dbo",
                table: "StockItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
