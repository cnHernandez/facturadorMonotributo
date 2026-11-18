using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace facturador.net.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCondicionIVAReceptor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponsableCondicionIVA",
                table: "Pacientes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CondicionIVAConfirmada",
                table: "ObrasSociales",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "CondicionIVA",
                table: "ObrasSociales",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponsableCondicionIVA",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "CondicionIVAConfirmada",
                table: "ObrasSociales");

            migrationBuilder.AlterColumn<int>(
                name: "CondicionIVA",
                table: "ObrasSociales",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
