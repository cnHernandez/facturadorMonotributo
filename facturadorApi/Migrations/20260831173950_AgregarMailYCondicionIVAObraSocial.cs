using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace facturador.net.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMailYCondicionIVAObraSocial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "Pacientes",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CondicionIVA",
                table: "ObrasSociales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Mail",
                table: "ObrasSociales",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "CondicionIVA",
                table: "ObrasSociales");

            migrationBuilder.DropColumn(
                name: "Mail",
                table: "ObrasSociales");
        }
    }
}
