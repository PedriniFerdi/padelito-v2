using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RequirePersonContactFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [People]
                    WHERE [Dni] IS NULL OR LEN(LTRIM(RTRIM([Dni]))) = 0
                       OR [Phone] IS NULL OR LEN(LTRIM(RTRIM([Phone]))) = 0
                       OR [Email] IS NULL OR LEN(LTRIM(RTRIM([Email]))) = 0)
                BEGIN
                    THROW 51000, 'No se puede aplicar la migracion: existen personas sin DNI, telefono o email. Complete esos datos primero.', 1;
                END
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "People",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "People",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Dni",
                table: "People",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_People_Dni_NotBlank",
                table: "People",
                sql: "LEN(LTRIM(RTRIM([Dni]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_People_Email_NotBlank",
                table: "People",
                sql: "LEN(LTRIM(RTRIM([Email]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_People_FirstName_NotBlank",
                table: "People",
                sql: "LEN(LTRIM(RTRIM([FirstName]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_People_LastName_NotBlank",
                table: "People",
                sql: "LEN(LTRIM(RTRIM([LastName]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_People_Phone_NotBlank",
                table: "People",
                sql: "LEN(LTRIM(RTRIM([Phone]))) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_People_Dni_NotBlank",
                table: "People");

            migrationBuilder.DropCheckConstraint(
                name: "CK_People_Email_NotBlank",
                table: "People");

            migrationBuilder.DropCheckConstraint(
                name: "CK_People_FirstName_NotBlank",
                table: "People");

            migrationBuilder.DropCheckConstraint(
                name: "CK_People_LastName_NotBlank",
                table: "People");

            migrationBuilder.DropCheckConstraint(
                name: "CK_People_Phone_NotBlank",
                table: "People");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "People",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "People",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Dni",
                table: "People",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
