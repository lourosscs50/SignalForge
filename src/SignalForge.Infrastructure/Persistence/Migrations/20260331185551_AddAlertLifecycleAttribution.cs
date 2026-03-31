using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertLifecycleAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcknowledgedByUserId",
                table: "alerts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReopenedAtUtc",
                table: "alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReopenedByUserId",
                table: "alerts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedByUserId",
                table: "alerts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcknowledgedByUserId",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "ReopenedAtUtc",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "ReopenedByUserId",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "ResolvedByUserId",
                table: "alerts");
        }
    }
}
