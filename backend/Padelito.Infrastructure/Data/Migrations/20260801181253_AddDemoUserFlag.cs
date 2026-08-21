using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoUserFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDemo",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDemo",
                table: "Users");
        }
    }
}
