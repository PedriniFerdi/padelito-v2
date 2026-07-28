using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations;

public partial class CompleteDemoSeed : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Historical migration ID retained. Demo business data must not be
        // provisioned by the production migration chain.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback must not delete records whose historical demo IDs may have
        // been reused or changed after deployment.
    }
}
