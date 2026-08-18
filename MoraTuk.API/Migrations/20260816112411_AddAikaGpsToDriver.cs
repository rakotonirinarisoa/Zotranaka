using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoraTuk.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAikaGpsToDriver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AikaDeviceId",
                table: "Drivers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AikaSerialNumber",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AikaDeviceId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "AikaSerialNumber",
                table: "Drivers");
        }
    }
}
