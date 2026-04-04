using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "decision_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecisionCategory = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DecisionType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TraceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AlertId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    SignalId = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyProfileKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StrategyPathKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderModelSummary = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    InputSummary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    OutputSummary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ExplanationAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    ExplanationSummary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ConfidenceBand = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FallbackUsageCount = table.Column<int>(type: "integer", nullable: true),
                    RetryUsageCount = table.Column<int>(type: "integer", nullable: true),
                    RecommendedActionSummary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    AuditActorUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decision_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_decision_records_CorrelationId",
                table: "decision_records",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_records_DecisionType",
                table: "decision_records",
                column: "DecisionType");

            migrationBuilder.CreateIndex(
                name: "IX_decision_records_ExecutionId",
                table: "decision_records",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_records_OccurredAtUtc",
                table: "decision_records",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_decision_records_RuleId",
                table: "decision_records",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_records_TraceId",
                table: "decision_records",
                column: "TraceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "decision_records");
        }
    }
}
