using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Padelito.Infrastructure.Data;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations;

[DbContext(typeof(PadelitoDbContext))]
[Migration("20260724190000_AddUsPortfolioDemoSeed")]
public sealed class AddUsPortfolioDemoSeed : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Historical migration ID retained. Portfolio data belongs in an
        // explicit development/demo workflow, never in production migrations.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Never delete fixed ID ranges during rollback.
    }
}
