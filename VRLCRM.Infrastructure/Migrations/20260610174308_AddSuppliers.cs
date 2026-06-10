using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VRLCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                schema: "dbo",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SupplierId",
                schema: "dbo",
                table: "Invoices",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Suppliers_SupplierId",
                schema: "dbo",
                table: "Invoices",
                column: "SupplierId",
                principalSchema: "dbo",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Suppliers_SupplierId",
                schema: "dbo",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_SupplierId",
                schema: "dbo",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                schema: "dbo",
                table: "Invoices");
        }
    }
}
