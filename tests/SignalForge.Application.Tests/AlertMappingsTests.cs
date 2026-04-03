using SignalForge.Application;
using SignalForge.Contracts.Alerts;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

/// <summary>Phase 5.3A–C: alert read-model mapping and shared lifecycle aggregation.</summary>
public sealed class AlertMappingsTests
{
    private static readonly AlertRuleSummary DummyRule = new(
        Guid.NewGuid(),
        "n",
        "SignalTypeEquals",
        "v",
        true,
        false,
        DateTimeOffset.UtcNow);

    private static readonly AlertSignalSummary DummySignal = new(
        Guid.NewGuid(),
        "s",
        "t",
        1.0,
        DateTimeOffset.UtcNow);

    [Fact]
    public void ToAlertResponse_and_ToAlertDetailResponse_share_derived_seconds_reopened_triage_and_age()
    {
        var created = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        var ackAt = created.AddSeconds(120);
        var reopenAt = created.AddHours(2);
        var readAt = created.AddHours(3);
        var alert = new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            created,
            IsAcknowledged: true,
            AcknowledgedAtUtc: ackAt,
            AcknowledgedByUserId: "a",
            IsResolved: false,
            ResolvedAtUtc: null,
            ResolvedByUserId: null,
            ReopenedAtUtc: reopenAt,
            ReopenedByUserId: "r");

        var compact = AlertMappings.ToAlertResponse(alert, readAt);
        var detail = AlertMappings.ToAlertDetailResponse(alert, DummyRule, DummySignal, readAt);

        Assert.Equal(compact.TimeToAcknowledgeSeconds, detail.TimeToAcknowledgeSeconds);
        Assert.Equal(120.0, compact.TimeToAcknowledgeSeconds);
        Assert.Null(compact.TimeToResolveSeconds);
        Assert.Null(detail.TimeToResolveSeconds);
        Assert.True(compact.HasBeenReopened);
        Assert.True(detail.HasBeenReopened);
        Assert.Equal(AlertMappings.StatusAcknowledged, compact.CurrentStatus);
        Assert.Equal(AlertMappings.StatusAcknowledged, detail.CurrentStatus);
        Assert.Equal(compact.AgeSeconds, detail.AgeSeconds);
        Assert.Equal(10800.0, compact.AgeSeconds, precision: 5);
    }

    [Fact]
    public void ToAlertResponse_TimeToResolveSeconds_and_CurrentStatus_Resolved()
    {
        var created = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
        var resolved = created.AddMinutes(10);
        var readAt = created.AddHours(1);
        var alert = new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            created,
            IsAcknowledged: false,
            AcknowledgedAtUtc: null,
            AcknowledgedByUserId: null,
            IsResolved: true,
            ResolvedAtUtc: resolved,
            ResolvedByUserId: "x");

        var r = AlertMappings.ToAlertResponse(alert, readAt);
        Assert.Equal(600.0, r.TimeToResolveSeconds!.Value, precision: 5);
        Assert.Null(r.TimeToAcknowledgeSeconds);
        Assert.False(r.HasBeenReopened);
        Assert.Equal(AlertMappings.StatusResolved, r.CurrentStatus);
    }

    [Fact]
    public void CurrentStatus_Open_when_unacked_and_unresolved()
    {
        var t0 = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), t0);
        var r = AlertMappings.ToAlertResponse(a, t0);
        Assert.Equal(AlertMappings.StatusOpen, r.CurrentStatus);
        Assert.Equal(0.0, r.AgeSeconds);
    }

    [Fact]
    public void AgeSeconds_clamps_to_zero_when_read_before_created()
    {
        var created = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var readBefore = created.AddMinutes(-5);
        var a = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), created);
        Assert.Equal(0.0, AlertMappings.AgeSeconds(a, readBefore));
    }

    [Fact]
    public void ToMetricsSummary_empty_dataset_has_zero_counts_and_null_avgs()
    {
        var s = AlertMappings.ToMetricsSummary([]);
        Assert.Equal(0, s.TotalAlerts);
        Assert.Equal(0, s.OpenAlerts);
        Assert.Equal(0, s.AcknowledgedUnresolvedAlerts);
        Assert.Equal(0, s.ResolvedAlerts);
        Assert.Equal(0, s.ReopenedAlerts);
        Assert.Null(s.AverageTimeToAcknowledgeSeconds);
        Assert.Null(s.AverageTimeToResolveSeconds);
    }

    [Fact]
    public void ToMetricsSummary_mixed_buckets_and_averages_skip_null_durations()
    {
        var t0 = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var open = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), t0);
        var ackUnres = new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            t0,
            true,
            t0.AddMinutes(1),
            "u1");
        var resolved = new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            t0,
            IsAcknowledged: false,
            AcknowledgedAtUtc: null,
            AcknowledgedByUserId: null,
            IsResolved: true,
            ResolvedAtUtc: t0.AddMinutes(4),
            ResolvedByUserId: "r1");
        var reopened = new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            t0,
            IsAcknowledged: true,
            AcknowledgedAtUtc: t0.AddMinutes(1),
            AcknowledgedByUserId: "u2",
            IsResolved: false,
            ResolvedAtUtc: null,
            ResolvedByUserId: null,
            ReopenedAtUtc: t0.AddHours(1),
            ReopenedByUserId: "o1");

        var list = new[] { open, ackUnres, resolved, reopened };
        var s = AlertMappings.ToMetricsSummary(list);

        Assert.Equal(4, s.TotalAlerts);
        Assert.Equal(1, s.OpenAlerts);
        Assert.Equal(2, s.AcknowledgedUnresolvedAlerts);
        Assert.Equal(1, s.ResolvedAlerts);
        Assert.Equal(1, s.ReopenedAlerts);

        Assert.Equal(60.0, s.AverageTimeToAcknowledgeSeconds!.Value, precision: 5);
        Assert.Equal(240.0, s.AverageTimeToResolveSeconds!.Value, precision: 5);
    }

    [Fact]
    public void ToMetricsSummary_reopened_unresolved_counts_reopened_not_resolved()
    {
        var t0 = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            t0,
            IsAcknowledged: false,
            AcknowledgedAtUtc: null,
            AcknowledgedByUserId: null,
            IsResolved: false,
            ResolvedAtUtc: null,
            ResolvedByUserId: null,
            ReopenedAtUtc: t0.AddHours(3),
            ReopenedByUserId: "z");

        var s = AlertMappings.ToMetricsSummary([a]);
        Assert.Equal(1, s.ReopenedAlerts);
        Assert.Equal(0, s.ResolvedAlerts);
        Assert.Equal(1, s.OpenAlerts);
    }

    [Fact]
    public void ToRuleMetricsSummary_matches_ToMetricsSummary_counts_and_avgs_and_includes_rule_identity()
    {
        var ruleId = Guid.NewGuid();
        var t0 = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var rule = new Rule(
            ruleId,
            "Rule-Z",
            RuleTypes.SignalTypeEquals,
            "z",
            true,
            false,
            t0);
        var a1 = new Alert(Guid.NewGuid(), Guid.NewGuid(), ruleId, t0, true, t0.AddSeconds(5), "u");
        var a2 = new Alert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ruleId,
            t0,
            IsAcknowledged: false,
            AcknowledgedAtUtc: null,
            AcknowledgedByUserId: null,
            IsResolved: true,
            ResolvedAtUtc: t0.AddSeconds(20),
            ResolvedByUserId: "r");

        var alerts = new[] { a1, a2 };
        var ruleMetrics = AlertMappings.ToRuleMetricsSummary(rule, alerts);
        var globalShape = AlertMappings.ToMetricsSummary(alerts);

        Assert.Equal(rule.Id, ruleMetrics.RuleId);
        Assert.Equal(rule.Name, ruleMetrics.RuleName);
        Assert.Equal(globalShape.TotalAlerts, ruleMetrics.TotalAlertsGenerated);
        Assert.Equal(globalShape.OpenAlerts, ruleMetrics.OpenAlerts);
        Assert.Equal(globalShape.AcknowledgedUnresolvedAlerts, ruleMetrics.AcknowledgedUnresolvedAlerts);
        Assert.Equal(globalShape.ResolvedAlerts, ruleMetrics.ResolvedAlerts);
        Assert.Equal(globalShape.ReopenedAlerts, ruleMetrics.ReopenedAlerts);
        Assert.Equal(globalShape.AverageTimeToAcknowledgeSeconds, ruleMetrics.AverageTimeToAcknowledgeSeconds);
        Assert.Equal(globalShape.AverageTimeToResolveSeconds, ruleMetrics.AverageTimeToResolveSeconds);
    }

    [Fact]
    public void ToRuleMetricsSummary_empty_alerts_zero_counts_null_avgs()
    {
        var t0 = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var rule = new Rule(
            Guid.NewGuid(),
            "Empty",
            RuleTypes.SignalTypeEquals,
            "e",
            true,
            false,
            t0);

        var m = AlertMappings.ToRuleMetricsSummary(rule, []);

        Assert.Equal(rule.Id, m.RuleId);
        Assert.Equal(rule.Name, m.RuleName);
        Assert.Equal(0, m.TotalAlertsGenerated);
        Assert.Null(m.AverageTimeToAcknowledgeSeconds);
        Assert.Null(m.AverageTimeToResolveSeconds);
    }
}
