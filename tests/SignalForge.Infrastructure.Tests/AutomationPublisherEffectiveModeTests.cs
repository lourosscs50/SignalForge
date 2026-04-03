using SignalForge.Infrastructure.Automation;
using SignalForge.Infrastructure.Integration;

namespace SignalForge.Infrastructure.Tests;

public sealed class AutomationPublisherEffectiveModeTests
{
    [Fact]
    public void EffectiveControlTriggerMode_uses_legacy_chronoflow_when_mode_unspecified()
    {
        var publishing = new AutomationPublishingOptions { ControlAutomationTriggers = null };
        var chron = new ChronoFlowControlTriggerIntegrationOptions
        {
            Enabled = true,
            BaseUrl = "https://cf.example/"
        };

        Assert.Equal(
            ControlAutomationTriggerPublisherMode.ChronoFlowHttp,
            AutomationPublisherRegistrations.EffectiveControlTriggerMode(publishing, chron));
    }

    [Fact]
    public void EffectiveControlTriggerMode_explicit_NoOp_does_not_use_legacy_http()
    {
        var publishing = new AutomationPublishingOptions
        {
            ControlAutomationTriggers = ControlAutomationTriggerPublisherMode.NoOp
        };
        var chron = new ChronoFlowControlTriggerIntegrationOptions
        {
            Enabled = true,
            BaseUrl = "https://cf.example/"
        };

        Assert.Equal(
            ControlAutomationTriggerPublisherMode.NoOp,
            AutomationPublisherRegistrations.EffectiveControlTriggerMode(publishing, chron));
    }

    [Fact]
    public void EffectiveControlTriggerMode_legacy_requires_non_empty_base_url()
    {
        var publishing = new AutomationPublishingOptions { ControlAutomationTriggers = null };
        var chron = new ChronoFlowControlTriggerIntegrationOptions
        {
            Enabled = true,
            BaseUrl = "  "
        };

        Assert.Equal(
            ControlAutomationTriggerPublisherMode.NoOp,
            AutomationPublisherRegistrations.EffectiveControlTriggerMode(publishing, chron));
    }

    [Fact]
    public void IsChronoFlowHttpDeliveryReady_requires_enabled_and_base_url()
    {
        Assert.False(AutomationPublisherRegistrations.IsChronoFlowHttpDeliveryReady(
            new ChronoFlowControlTriggerIntegrationOptions
            {
                Enabled = true,
                BaseUrl = ""
            }));
        Assert.False(AutomationPublisherRegistrations.IsChronoFlowHttpDeliveryReady(
            new ChronoFlowControlTriggerIntegrationOptions
            {
                Enabled = false,
                BaseUrl = "https://x/"
            }));
        Assert.True(AutomationPublisherRegistrations.IsChronoFlowHttpDeliveryReady(
            new ChronoFlowControlTriggerIntegrationOptions
            {
                Enabled = true,
                BaseUrl = "https://x/"
            }));
    }
}
