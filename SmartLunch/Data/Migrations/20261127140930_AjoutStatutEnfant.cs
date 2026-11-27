using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLunch.Data.Migrations
{
    /// <inheritdoc />
    public partial class AjoutStatutEnfant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Children",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Children",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Children");
        }
    }
}
