using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumFlagToBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPremiumOnly",
                table: "Books",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPremiumOnly",
                table: "Books");
        }
    }
}
