using SignalForge.Domain;

namespace SignalForge.Domain.Tests;

public sealed class DomainModelTests
{
    [Fact]
    public void Signal_preserves_Value_when_set()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var s = new Signal(id, "s", "type", "", 42.5, t, t);

        Assert.Equal(42.5, s.Value);
    }

    [Fact]
    public void Signal_allows_null_Value()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var s = new Signal(id, "s", "type", "", null, t, t);

        Assert.Null(s.Value);
    }

    [Fact]
    public void Rule_preserves_RuleType_and_MatchValue()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var r = new Rule(id, "n", RuleTypes.SignalTypeEquals, "temperature", true, t);

        Assert.Equal(RuleTypes.SignalTypeEquals, r.RuleType);
        Assert.Equal("temperature", r.MatchValue);
    }

    [Fact]
    public void Alert_links_SignalId_and_RuleId()
    {
        var alertId = Guid.NewGuid();
        var signalId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var a = new Alert(alertId, signalId, ruleId, t);

        Assert.Equal(signalId, a.SignalId);
        Assert.Equal(ruleId, a.RuleId);
    }
}
