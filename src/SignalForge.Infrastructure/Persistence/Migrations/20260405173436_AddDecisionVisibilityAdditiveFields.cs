using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionVisibilityAdditiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChronoFlowExecutionInstanceId",
                table: "decision_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionOptions",
                table: "decision_records",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedOptionId",
                table: "decision_records",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChronoFlowExecutionInstanceId",
                table: "decision_records");

            migrationBuilder.DropColumn(
                name: "DecisionOptions",
                table: "decision_records");

            migrationBuilder.DropColumn(
                name: "SelectedOptionId",
                table: "decision_records");
        }
    }
}
