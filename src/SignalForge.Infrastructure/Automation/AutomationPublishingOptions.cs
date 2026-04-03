namespace SignalForge.Infrastructure.Automation;

/// <summary>Selects concrete <see cref="SignalForge.Application.IAlertLifecycleEventPublisher"/> and
/// <see cref="SignalForge.Application.IControlAutomationTriggerPublisher"/> implementations (infrastructure-only).</summary>
public sealed class AutomationPublishingOptions
{
    public const string SectionName = "Automation:Publishing";

    /// <summary>How alert lifecycle events are surfaced after real transitions.</summary>
    public AlertLifecycleEventPublisherMode AlertLifecycleEvents { get; set; } = AlertLifecycleEventPublisherMode.NoOp;

    /// <summary>
    /// How control automation triggers are published. When omitted, ChronoFlow HTTP is used when
    /// <c>ChronoFlow:ControlTriggers</c> is enabled with a non-empty base URL; otherwise <see cref="ControlAutomationTriggerPublisherMode.NoOp"/>.
    /// </summary>
    public ControlAutomationTriggerPublisherMode? ControlAutomationTriggers { get; set; }
}
