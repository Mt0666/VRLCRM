using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VRLCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSalesInvoiceLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SalesInvoiceId",
                schema: "dbo",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SalesInvoiceId",
                schema: "dbo",
                table: "Orders",
                column: "SalesInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Invoices_SalesInvoiceId",
                schema: "dbo",
                table: "Orders",
                column: "SalesInvoiceId",
                principalSchema: "dbo",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Invoices_SalesInvoiceId",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SalesInvoiceId",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SalesInvoiceId",
                schema: "dbo",
                table: "Orders");
        }
    }
}
