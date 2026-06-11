using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VRLCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorOrdersInvoicesVat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                schema: "dbo",
                table: "StockItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                schema: "dbo",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatTotal",
                schema: "dbo",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                schema: "dbo",
                table: "OrderLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                schema: "dbo",
                table: "OrderLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                schema: "dbo",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatTotal",
                schema: "dbo",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                schema: "dbo",
                table: "InvoiceLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                schema: "dbo",
                table: "InvoiceLines",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatRate",
                schema: "dbo",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VatTotal",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                schema: "dbo",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "VatRate",
                schema: "dbo",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                schema: "dbo",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VatTotal",
                schema: "dbo",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                schema: "dbo",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "VatRate",
                schema: "dbo",
                table: "InvoiceLines");
        }
    }
}
