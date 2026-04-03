namespace SignalForge.Infrastructure.Automation;

public enum AlertLifecycleEventPublisherMode
{
    NoOp = 0,
    Logging = 1
}

public enum ControlAutomationTriggerPublisherMode
{
    NoOp = 0,
    Logging = 1,
    ChronoFlowHttp = 2
}
