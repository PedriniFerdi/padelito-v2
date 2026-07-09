using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetAdminDemoPassword : Migration
    {
        private const string DemoPasswordHash = "AQAAAAIAAYagAAAAED2SFjyZfFosfjAmmH1n5FHdE59w+9e6K96p468HR/FvY6jo4v94M+pMCLf/9mpNhA==";
        private const string PreviousPasswordHash = "AQAAAAIAAYagAAAAEKlUxApUaC++Cpt9h52jpYYoOh5rsBiS+qS16LsV4dmOmG5Yc8vYpEJT0IAght085A==";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE [Users]
                SET [PasswordHash] = N'{DemoPasswordHash}'
                WHERE [Id] = 1 AND [Username] = N'admin';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE [Users]
                SET [PasswordHash] = N'{PreviousPasswordHash}'
                WHERE [Id] = 1 AND [Username] = N'admin';
                """);
        }
    }
}
