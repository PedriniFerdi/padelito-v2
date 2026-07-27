using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations;

public partial class SetAdminDemoPassword : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Historical migration ID retained for databases that already recorded it.
        // Production bootstrap owns administrator credential provisioning.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Never rewrite an administrator credential during rollback.
    }
}
