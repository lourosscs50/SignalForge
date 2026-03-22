using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleAuditUpdateDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "update_new_match_value",
                table: "rule_audit_entries",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "update_new_name",
                table: "rule_audit_entries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "update_previous_match_value",
                table: "rule_audit_entries",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "update_previous_name",
                table: "rule_audit_entries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "update_new_match_value",
                table: "rule_audit_entries");

            migrationBuilder.DropColumn(
                name: "update_new_name",
                table: "rule_audit_entries");

            migrationBuilder.DropColumn(
                name: "update_previous_match_value",
                table: "rule_audit_entries");

            migrationBuilder.DropColumn(
                name: "update_previous_name",
                table: "rule_audit_entries");
        }
    }
}
