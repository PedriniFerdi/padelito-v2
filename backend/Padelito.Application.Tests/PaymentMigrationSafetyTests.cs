using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Padelito.Infrastructure.Data.Migrations;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class PaymentMigrationSafetyTests
{
    [Fact]
    public void Up_fails_closed_before_enforcing_the_unique_index()
    {
        var operations = BuildOperations("Up");

        Assert.Collection(
            operations,
            operation =>
            {
                var guard = Assert.IsType<SqlOperation>(operation);
                Assert.Contains("IF EXISTS", guard.Sql, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("COUNT_BIG(*) <> 1", guard.Sql, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("SUM(CONVERT(decimal(38, 2)", guard.Sql, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("THROW 51003", guard.Sql, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("DELETE", guard.Sql, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("UPDATE", guard.Sql, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("MERGE", guard.Sql, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("INSERT", guard.Sql, StringComparison.OrdinalIgnoreCase);
                Assert.False(guard.SuppressTransaction);
            },
            operation =>
            {
                var drop = Assert.IsType<DropIndexOperation>(operation);
                Assert.Equal("IX_Payments_ReservationId", drop.Name);
                Assert.Equal("Payments", drop.Table);
            },
            operation =>
            {
                var create = Assert.IsType<CreateIndexOperation>(operation);
                Assert.Equal("IX_Payments_ReservationId", create.Name);
                Assert.Equal("Payments", create.Table);
                Assert.Equal(["ReservationId"], create.Columns);
                Assert.True(create.IsUnique);
            });
    }

    [Fact]
    public void Down_changes_only_index_cardinality()
    {
        var operations = BuildOperations("Down");

        Assert.Collection(
            operations,
            operation =>
            {
                var drop = Assert.IsType<DropIndexOperation>(operation);
                Assert.Equal("IX_Payments_ReservationId", drop.Name);
                Assert.Equal("Payments", drop.Table);
            },
            operation =>
            {
                var create = Assert.IsType<CreateIndexOperation>(operation);
                Assert.Equal("IX_Payments_ReservationId", create.Name);
                Assert.Equal("Payments", create.Table);
                Assert.Equal(["ReservationId"], create.Columns);
                Assert.False(create.IsUnique);
            });
    }

    private static IReadOnlyList<MigrationOperation> BuildOperations(string methodName)
    {
        var migration = new EnforceFullReservationPayment();
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Migration has no {methodName} method.");
        method.Invoke(migration, [builder]);
        return builder.Operations;
    }
}
