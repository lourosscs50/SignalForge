using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application;

internal static class RuleAuditMappings
{
    public static RuleAuditEntryResponse ToResponse(RuleAuditEntry entry) =>
        new(
            entry.Id,
            entry.RuleId,
            entry.Action,
            new DateTimeOffset(entry.OccurredAtUtc, TimeSpan.Zero));
}
