using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaginationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Location indexes for common sort patterns
            migrationBuilder.CreateIndex(
                name: "IX_Locations_Name_Id",
                table: "Locations",
                columns: new[] { "Name", "Id" });
            
            migrationBuilder.CreateIndex(
                name: "IX_Locations_CreatedAt_Id",
                table: "Locations",
                columns: new[] { "CreatedAt", "Id" });
            
            // Item indexes for common sort patterns
            migrationBuilder.CreateIndex(
                name: "IX_Items_Name_Id",
                table: "Items",
                columns: new[] { "Name", "Id" });
            
            migrationBuilder.CreateIndex(
                name: "IX_Items_Quantity_Id",
                table: "Items",
                columns: new[] { "Quantity", "Id" });
            
            migrationBuilder.CreateIndex(
                name: "IX_Items_CreatedAt_Id",
                table: "Items",
                columns: new[] { "CreatedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Locations_Name_Id", table: "Locations");
            migrationBuilder.DropIndex(name: "IX_Locations_CreatedAt_Id", table: "Locations");
            migrationBuilder.DropIndex(name: "IX_Items_Name_Id", table: "Items");
            migrationBuilder.DropIndex(name: "IX_Items_Quantity_Id", table: "Items");
            migrationBuilder.DropIndex(name: "IX_Items_CreatedAt_Id", table: "Items");
        }
    }
}
