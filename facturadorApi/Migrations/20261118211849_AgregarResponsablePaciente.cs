using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace facturador.net.Migrations
{
    /// <inheritdoc />
    public partial class AgregarResponsablePaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponsableApellido",
                table: "Pacientes",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ResponsableDni",
                table: "Pacientes",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ResponsableDomicilio",
                table: "Pacientes",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ResponsableNombre",
                table: "Pacientes",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ResponsableVinculo",
                table: "Pacientes",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponsableApellido",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "ResponsableDni",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "ResponsableDomicilio",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "ResponsableNombre",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "ResponsableVinculo",
                table: "Pacientes");

        }
    }
}
