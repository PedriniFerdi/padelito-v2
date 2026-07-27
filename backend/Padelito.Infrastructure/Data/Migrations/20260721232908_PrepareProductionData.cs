using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations
{
    public partial class PrepareProductionData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Historical migration ID retained. Never delete application,
            // financial, personal, or audit data during schema deployment.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback must not recreate environment-specific demo data.
        }
    }
}
