using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertAcknowledgment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAtUtc",
                table: "alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAcknowledged",
                table: "alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcknowledgedAtUtc",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "IsAcknowledged",
                table: "alerts");
        }
    }
}
