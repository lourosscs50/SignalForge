using System.Text.Json;
using SignalForge.Contracts.Automation;

namespace SignalForge.Infrastructure.Tests;

/// <summary>Golden JSON must stay aligned with ChronoFlow <c>ReceiveControlTriggerRequest</c> binding.</summary>
public sealed class ControlAutomationTriggerContractAlignmentTests
{
    [Fact]
    public void Serialized_payload_uses_expected_camelCase_fields_for_chronoflow_intake()
    {
        var request = new ControlAutomationTriggerRequest(
            "AlertReopened",
            Guid.Parse("f1d56d1a-0000-4000-8000-000000000001"),
            Guid.Parse("f1d56d1a-0000-4000-8000-000000000002"),
            Guid.Parse("f1d56d1a-0000-4000-8000-000000000003"),
            new DateTimeOffset(2026, 3, 15, 8, 30, 0, TimeSpan.Zero),
            "Open",
            "AlertReopened",
            "user-a",
            null,
            "user-r",
            "Heat rule",
            true);

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(12, root.EnumerateObject().Count());
        Assert.True(root.TryGetProperty("triggerType", out _));
        Assert.True(root.TryGetProperty("alertId", out _));
        Assert.True(root.TryGetProperty("ruleId", out _));
        Assert.True(root.TryGetProperty("signalId", out _));
        Assert.True(root.TryGetProperty("occurredAtUtc", out _));
        Assert.True(root.TryGetProperty("currentStatus", out _));
        Assert.True(root.TryGetProperty("lifecycleEventType", out _));
        Assert.True(root.TryGetProperty("acknowledgedByUserId", out _));
        Assert.True(root.TryGetProperty("resolvedByUserId", out _));
        Assert.True(root.TryGetProperty("reopenedByUserId", out _));
        Assert.True(root.TryGetProperty("ruleName", out _));
        Assert.True(root.TryGetProperty("hasBeenReopened", out _));
    }
}
