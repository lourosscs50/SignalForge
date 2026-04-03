using Microsoft.Extensions.Logging.Abstractions;
using SignalForge.Contracts.Automation;
using SignalForge.Infrastructure.Automation;

namespace SignalForge.Infrastructure.Tests;

public sealed class LoggingAutomationPublishersTests
{
    private static readonly DateTimeOffset T = new(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoggingAlertLifecycleEventPublisher accepts_payload_without_throwing()
    {
        var sut = new LoggingAlertLifecycleEventPublisher(NullLogger<LoggingAlertLifecycleEventPublisher>.Instance);
        var evt = new AlertLifecycleEvent(
            AlertLifecycleTransitionType.AlertAcknowledged,
            Guid.Parse("a1000000-0000-0000-0000-000000000001"),
            Guid.Parse("b2000000-0000-0000-0000-000000000002"),
            Guid.Parse("c3000000-0000-0000-0000-000000000003"),
            T,
            "Open",
            true,
            false,
            T,
            null,
            null,
            "u1",
            null,
            null,
            "Rule Z");

        var ex = await Record.ExceptionAsync(() => sut.PublishAsync(evt, CancellationToken.None));
        Assert.Null(ex);
    }

    [Fact]
    public async Task LoggingControlAutomationTriggerPublisher_accepts_payload_without_throwing()
    {
        var sut = new LoggingControlAutomationTriggerPublisher(NullLogger<LoggingControlAutomationTriggerPublisher>.Instance);
        var req = new ControlAutomationTriggerRequest(
            "AlertCreated",
            Guid.Parse("a1000000-0000-0000-0000-000000000001"),
            Guid.Parse("b2000000-0000-0000-0000-000000000002"),
            Guid.Parse("c3000000-0000-0000-0000-000000000003"),
            T,
            "Open",
            "AlertCreated",
            null,
            null,
            null,
            "R",
            false);

        var ex = await Record.ExceptionAsync(() => sut.PublishAsync(req, CancellationToken.None));
        Assert.Null(ex);
    }
}
