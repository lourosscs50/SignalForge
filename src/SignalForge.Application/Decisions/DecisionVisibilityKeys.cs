namespace SignalForge.Application.Decisions;

public static class DecisionVisibilityKeys
{
    public const string CategoryAlertLifecycle = "alert.lifecycle";

    public const string StatusSucceeded = "succeeded";

    public static class Types
    {
        public const string AlertCreated = "alert.created";
        public const string AlertAcknowledged = "alert.acknowledged";
        public const string AlertResolved = "alert.resolved";
        public const string AlertReopened = "alert.reopened";
    }
}
