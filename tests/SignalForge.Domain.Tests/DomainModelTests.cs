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
        var r = new Rule(id, "n", RuleTypes.SignalTypeEquals, "temperature", true, false, t);

        Assert.Equal(RuleTypes.SignalTypeEquals, r.RuleType);
        Assert.Equal("temperature", r.MatchValue);
    }

    [Fact]
    public void Rule_Activate_is_idempotent_when_already_active()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var r = new Rule(id, "n", RuleTypes.SignalTypeEquals, "x", true, false, t);

        var a = r.Activate();
        Assert.True(a.IsActive);
        var b = a.Activate();
        Assert.True(b.IsActive);
    }

    [Fact]
    public void Rule_Deactivate_is_idempotent_when_already_inactive()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var r = new Rule(id, "n", RuleTypes.SignalTypeEquals, "x", false, false, t);

        var a = r.Deactivate();
        Assert.False(a.IsActive);
        var b = a.Deactivate();
        Assert.False(b.IsActive);
    }

    [Fact]
    public void Rule_Activate_and_Deactivate_toggle_IsActive()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var active = new Rule(id, "n", RuleTypes.SignalTypeEquals, "x", true, false, t);
        Assert.False(active.Deactivate().IsActive);
        Assert.True(active.Deactivate().Activate().IsActive);
    }

    [Fact]
    public void Rule_UpdateDetails_changes_name_and_match_only()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var r = new Rule(id, "old", RuleTypes.SignalTypeEquals, "a", false, false, t);

        var u = r.UpdateDetails("new", "b");

        Assert.Equal(id, u.Id);
        Assert.Equal(RuleTypes.SignalTypeEquals, u.RuleType);
        Assert.False(u.IsActive);
        Assert.False(u.IsArchived);
        Assert.Equal(t, u.CreatedAtUtc);
        Assert.Equal("new", u.Name);
        Assert.Equal("b", u.MatchValue);
    }

    [Fact]
    public void Rule_Archive_sets_archived_and_deactivates()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var r = new Rule(id, "n", RuleTypes.SignalTypeEquals, "x", true, false, t);

        var a = r.Archive();

        Assert.True(a.IsArchived);
        Assert.False(a.IsActive);
    }

    [Fact]
    public void Rule_Archive_is_idempotent()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var r = new Rule(id, "n", RuleTypes.SignalTypeEquals, "x", false, true, t);

        var a = r.Archive();
        Assert.Same(r, a);
    }

    [Fact]
    public void Rule_Activate_throws_when_archived()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var r = new Rule(id, "n", RuleTypes.SignalTypeEquals, "x", false, true, t);

        Assert.Throws<InvalidOperationException>(() => r.Activate());
    }

    [Fact]
    public void Rule_Unarchive_clears_archive_and_keeps_inactive()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var r = new Rule(id, "n", RuleTypes.SignalTypeEquals, "x", false, true, t);

        var u = r.Unarchive();

        Assert.False(u.IsArchived);
        Assert.False(u.IsActive);
        Assert.Equal(id, u.Id);
        Assert.Equal(t, u.CreatedAtUtc);
    }

    [Fact]
    public void Rule_Unarchive_is_idempotent_when_not_archived()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var r = new Rule(id, "n", RuleTypes.SignalTypeEquals, "x", false, false, t);

        var u = r.Unarchive();
        Assert.Same(r, u);
    }

    [Fact]
    public void Rule_Unarchive_then_Activate_succeeds()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var archived = new Rule(id, "n", RuleTypes.SignalTypeEquals, "x", false, true, t);

        var restored = archived.Unarchive();
        var active = restored.Activate();

        Assert.True(active.IsActive);
        Assert.False(active.IsArchived);
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
