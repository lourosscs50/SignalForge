using SignalForge.Contracts.Alerts;
using SignalForge.Domain;

namespace SignalForge.Application;

internal static class AlertDetailMappings
{
    public static AlertRuleSummary ToAlertRuleSummary(Rule rule) =>
        new(
            rule.Id,
            rule.Name,
            rule.RuleType,
            rule.MatchValue,
            rule.IsActive,
            rule.IsArchived,
            new DateTimeOffset(rule.CreatedAtUtc, TimeSpan.Zero));

    public static AlertSignalSummary ToAlertSignalSummary(Signal signal) =>
        new(
            signal.Id,
            signal.Source,
            signal.Type,
            signal.Value,
            new DateTimeOffset(signal.OccurredAtUtc, TimeSpan.Zero));
}
