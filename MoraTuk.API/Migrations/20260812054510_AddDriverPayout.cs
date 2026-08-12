using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoraTuk.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverPayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DriverPayoutId",
                table: "DriverEarnings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DriverPayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    PayoutDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalRides = table.Column<int>(type: "int", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WaitingFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DriverAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServerCorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverPayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverPayouts_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverEarnings_DriverPayoutId",
                table: "DriverEarnings",
                column: "DriverPayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverPayouts_DriverId",
                table: "DriverPayouts",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverEarnings_DriverPayouts_DriverPayoutId",
                table: "DriverEarnings",
                column: "DriverPayoutId",
                principalTable: "DriverPayouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverEarnings_DriverPayouts_DriverPayoutId",
                table: "DriverEarnings");

            migrationBuilder.DropTable(
                name: "DriverPayouts");

            migrationBuilder.DropIndex(
                name: "IX_DriverEarnings_DriverPayoutId",
                table: "DriverEarnings");

            migrationBuilder.DropColumn(
                name: "DriverPayoutId",
                table: "DriverEarnings");
        }
    }
}
