using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InmindAi.Workshop.Logging.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApplyConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_Reference",
                table: "Orders",
                column: "Reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_Reference",
                table: "Orders");
        }
    }
}
