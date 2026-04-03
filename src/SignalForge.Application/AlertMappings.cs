using SignalForge.Contracts.Alerts;
using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application;

internal static class AlertMappings
{
    public const string StatusResolved = "Resolved";
    public const string StatusAcknowledged = "Acknowledged";
    public const string StatusOpen = "Open";

    public static double? TimeToAcknowledgeSeconds(Alert alert) =>
        alert.AcknowledgedAtUtc is { } ack
            ? (ack - alert.CreatedAtUtc).TotalSeconds
            : null;

    public static double? TimeToResolveSeconds(Alert alert) =>
        alert.ResolvedAtUtc is { } res
            ? (res - alert.CreatedAtUtc).TotalSeconds
            : null;

    public static bool HasBeenReopened(Alert alert) => alert.ReopenedAtUtc.HasValue;

    public static string CurrentStatus(Alert alert) =>
        alert.IsResolved ? StatusResolved
            : alert.IsAcknowledged ? StatusAcknowledged
            : StatusOpen;

    public static double AgeSeconds(Alert alert, DateTime utcNow) =>
        Math.Max(0, (utcNow - alert.CreatedAtUtc).TotalSeconds);

    public static AlertResponse ToAlertResponse(Alert alert, DateTime utcNow) =>
        new(
            alert.Id,
            alert.SignalId,
            alert.RuleId,
            new DateTimeOffset(alert.CreatedAtUtc, TimeSpan.Zero),
            alert.IsAcknowledged,
            alert.AcknowledgedAtUtc.HasValue
                ? new DateTimeOffset(alert.AcknowledgedAtUtc.Value, TimeSpan.Zero)
                : null,
            alert.AcknowledgedByUserId,
            alert.IsResolved,
            alert.ResolvedAtUtc.HasValue
                ? new DateTimeOffset(alert.ResolvedAtUtc.Value, TimeSpan.Zero)
                : null,
            alert.ResolvedByUserId,
            alert.ReopenedAtUtc.HasValue
                ? new DateTimeOffset(alert.ReopenedAtUtc.Value, TimeSpan.Zero)
                : null,
            alert.ReopenedByUserId,
            TimeToAcknowledgeSeconds(alert),
            TimeToResolveSeconds(alert),
            HasBeenReopened(alert),
            CurrentStatus(alert),
            AgeSeconds(alert, utcNow));

    public static AlertDetailResponse ToAlertDetailResponse(
        Alert alert,
        AlertRuleSummary rule,
        AlertSignalSummary signal,
        DateTime utcNow) =>
        new(
            Id: alert.Id,
            SignalId: alert.SignalId,
            RuleId: alert.RuleId,
            CreatedAtUtc: new DateTimeOffset(alert.CreatedAtUtc, TimeSpan.Zero),
            IsAcknowledged: alert.IsAcknowledged,
            AcknowledgedAtUtc: alert.AcknowledgedAtUtc.HasValue
                ? new DateTimeOffset(alert.AcknowledgedAtUtc.Value, TimeSpan.Zero)
                : null,
            AcknowledgedByUserId: alert.AcknowledgedByUserId,
            IsResolved: alert.IsResolved,
            ResolvedAtUtc: alert.ResolvedAtUtc.HasValue
                ? new DateTimeOffset(alert.ResolvedAtUtc.Value, TimeSpan.Zero)
                : null,
            ResolvedByUserId: alert.ResolvedByUserId,
            ReopenedAtUtc: alert.ReopenedAtUtc.HasValue
                ? new DateTimeOffset(alert.ReopenedAtUtc.Value, TimeSpan.Zero)
                : null,
            ReopenedByUserId: alert.ReopenedByUserId,
            TimeToAcknowledgeSeconds: TimeToAcknowledgeSeconds(alert),
            TimeToResolveSeconds: TimeToResolveSeconds(alert),
            HasBeenReopened: HasBeenReopened(alert),
            CurrentStatus: CurrentStatus(alert),
            AgeSeconds: AgeSeconds(alert, utcNow),
            Rule: rule,
            Signal: signal);

    public static AlertMetricsSummaryResponse ToMetricsSummary(IReadOnlyList<Alert> alerts)
    {
        var s = ComputeLifecycleSnapshot(alerts);
        return new AlertMetricsSummaryResponse(
            TotalAlerts: s.Total,
            OpenAlerts: s.Open,
            AcknowledgedUnresolvedAlerts: s.AckUnresolved,
            ResolvedAlerts: s.Resolved,
            ReopenedAlerts: s.Reopened,
            AverageTimeToAcknowledgeSeconds: s.AverageAck,
            AverageTimeToResolveSeconds: s.AverageResolve);
    }

    public static RuleMetricsSummaryResponse ToRuleMetricsSummary(Rule rule, IReadOnlyList<Alert> alerts)
    {
        var s = ComputeLifecycleSnapshot(alerts);
        return new RuleMetricsSummaryResponse(
            RuleId: rule.Id,
            RuleName: rule.Name,
            TotalAlertsGenerated: s.Total,
            OpenAlerts: s.Open,
            AcknowledgedUnresolvedAlerts: s.AckUnresolved,
            ResolvedAlerts: s.Resolved,
            ReopenedAlerts: s.Reopened,
            AverageTimeToAcknowledgeSeconds: s.AverageAck,
            AverageTimeToResolveSeconds: s.AverageResolve);
    }

    private static (
        int Total,
        int Open,
        int AckUnresolved,
        int Resolved,
        int Reopened,
        double? AverageAck,
        double? AverageResolve) ComputeLifecycleSnapshot(IReadOnlyList<Alert> alerts)
    {
        var total = alerts.Count;
        var open = 0;
        var ackUnresolved = 0;
        var resolved = 0;
        var reopened = 0;
        double ackSum = 0;
        var ackCount = 0;
        double resSum = 0;
        var resCount = 0;

        foreach (var a in alerts)
        {
            if (!a.IsResolved && !a.IsAcknowledged)
                open++;
            if (a.IsAcknowledged && !a.IsResolved)
                ackUnresolved++;
            if (a.IsResolved)
                resolved++;
            if (a.ReopenedAtUtc.HasValue)
                reopened++;

            var tta = TimeToAcknowledgeSeconds(a);
            if (tta.HasValue)
            {
                ackSum += tta.Value;
                ackCount++;
            }

            var ttr = TimeToResolveSeconds(a);
            if (ttr.HasValue)
            {
                resSum += ttr.Value;
                resCount++;
            }
        }

        return (
            total,
            open,
            ackUnresolved,
            resolved,
            reopened,
            ackCount == 0 ? null : ackSum / ackCount,
            resCount == 0 ? null : resSum / resCount);
    }
}
