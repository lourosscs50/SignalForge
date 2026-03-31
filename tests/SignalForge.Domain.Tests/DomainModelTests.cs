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

    [Fact]
    public void Alert_Acknowledge_sets_IsAcknowledged_and_AcknowledgedAtUtc_and_AcknowledgedByUserId()
    {
        var t0 = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var ackAt = new DateTime(2025, 6, 7, 8, 9, 10, DateTimeKind.Utc);
        var a = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), t0);

        var ack = a.Acknowledge(ackAt, "actor-ack");

        Assert.True(ack.IsAcknowledged);
        Assert.Equal(ackAt, ack.AcknowledgedAtUtc);
        Assert.Equal("actor-ack", ack.AcknowledgedByUserId);
        Assert.Equal(t0, ack.CreatedAtUtc);
    }

    [Fact]
    public void Alert_Acknowledge_is_idempotent_and_preserves_first_AcknowledgedAtUtc_and_AcknowledgedByUserId()
    {
        var first = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var second = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var a = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var once = a.Acknowledge(first, "first-actor");
        var twice = once.Acknowledge(second, "other-actor");

        Assert.Same(once, twice);
        Assert.Equal(first, twice.AcknowledgedAtUtc);
        Assert.Equal("first-actor", twice.AcknowledgedByUserId);
    }

    [Fact]
    public void Alert_Resolve_sets_IsResolved_and_ResolvedAtUtc_and_ResolvedByUserId_without_acknowledging()
    {
        var created = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var resAt = new DateTime(2025, 8, 15, 12, 0, 0, DateTimeKind.Utc);
        var signalId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var a = new Alert(id, signalId, ruleId, created);

        var r = a.Resolve(resAt, "actor-res");

        Assert.True(r.IsResolved);
        Assert.Equal(resAt, r.ResolvedAtUtc);
        Assert.Equal("actor-res", r.ResolvedByUserId);
        Assert.False(r.IsAcknowledged);
        Assert.Null(r.AcknowledgedAtUtc);
        Assert.Null(r.AcknowledgedByUserId);
        Assert.Equal(id, r.Id);
        Assert.Equal(signalId, r.SignalId);
        Assert.Equal(ruleId, r.RuleId);
        Assert.Equal(created, r.CreatedAtUtc);
    }

    [Fact]
    public void Alert_Resolve_preserves_acknowledgment_and_AcknowledgedByUserId_when_already_acknowledged()
    {
        var ackAt = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var resAt = new DateTime(2025, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        var a = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var acked = a.Acknowledge(ackAt, "ack-u");
        var resolved = acked.Resolve(resAt, "res-u");

        Assert.True(resolved.IsResolved);
        Assert.Equal(resAt, resolved.ResolvedAtUtc);
        Assert.Equal("res-u", resolved.ResolvedByUserId);
        Assert.True(resolved.IsAcknowledged);
        Assert.Equal(ackAt, resolved.AcknowledgedAtUtc);
        Assert.Equal("ack-u", resolved.AcknowledgedByUserId);
    }

    [Fact]
    public void Alert_Resolve_is_idempotent_and_preserves_ResolvedAtUtc_and_ResolvedByUserId()
    {
        var first = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var second = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var a = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var once = a.Resolve(first, "resolver-a");
        var twice = once.Resolve(second, "resolver-b");

        Assert.Same(once, twice);
        Assert.Equal(first, twice.ResolvedAtUtc);
        Assert.Equal("resolver-a", twice.ResolvedByUserId);
    }

    [Fact]
    public void Alert_Reopen_clears_resolved_attribution_sets_reopen_attribution_preserves_acknowledgment()
    {
        var ackAt = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var resAt = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var reopenAt = new DateTime(2025, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var a = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var resolved = a.Acknowledge(ackAt, "ack-r").Resolve(resAt, "res-r");

        var reopened = resolved.Reopen(reopenAt, "reopen-r");

        Assert.False(reopened.IsResolved);
        Assert.Null(reopened.ResolvedAtUtc);
        Assert.Null(reopened.ResolvedByUserId);
        Assert.Equal(reopenAt, reopened.ReopenedAtUtc);
        Assert.Equal("reopen-r", reopened.ReopenedByUserId);
        Assert.True(reopened.IsAcknowledged);
        Assert.Equal(ackAt, reopened.AcknowledgedAtUtc);
        Assert.Equal("ack-r", reopened.AcknowledgedByUserId);
    }

    [Fact]
    public void Alert_Reopen_is_idempotent_when_already_unresolved_preserves_Reopened_attribution()
    {
        var reopenAt = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var later = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var acked = a.Acknowledge(DateTime.UtcNow, "a1");
        var onceReopened = acked.Resolve(DateTime.UtcNow, "r1").Reopen(reopenAt, "o1");
        var twiceReopened = onceReopened.Reopen(later, "o2");

        Assert.Same(onceReopened, twiceReopened);
        Assert.Equal(reopenAt, twiceReopened.ReopenedAtUtc);
        Assert.Equal("o1", twiceReopened.ReopenedByUserId);
    }

    [Fact]
    public void Alert_Reopen_preserves_Id_SignalId_RuleId_CreatedAtUtc()
    {
        var created = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var signalId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var resAt = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var reopenAt = new DateTime(2025, 7, 2, 0, 0, 0, DateTimeKind.Utc);
        var resolved = new Alert(id, signalId, ruleId, created).Resolve(resAt, "x");

        var reopened = resolved.Reopen(reopenAt, "y");

        Assert.Equal(id, reopened.Id);
        Assert.Equal(signalId, reopened.SignalId);
        Assert.Equal(ruleId, reopened.RuleId);
        Assert.Equal(created, reopened.CreatedAtUtc);
        Assert.False(reopened.IsResolved);
        Assert.Null(reopened.ResolvedAtUtc);
    }

    [Fact]
    public void Alert_Reopen_on_resolved_without_ack_preserves_unacked_state_and_sets_reopen_attribution()
    {
        var resAt = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var reopenAt = new DateTime(2025, 8, 2, 0, 0, 0, DateTimeKind.Utc);
        var resolved = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow).Resolve(resAt, "res");

        var reopened = resolved.Reopen(reopenAt, "op");

        Assert.False(reopened.IsResolved);
        Assert.Null(reopened.ResolvedAtUtc);
        Assert.Null(reopened.ResolvedByUserId);
        Assert.Equal(reopenAt, reopened.ReopenedAtUtc);
        Assert.Equal("op", reopened.ReopenedByUserId);
        Assert.False(reopened.IsAcknowledged);
        Assert.Null(reopened.AcknowledgedAtUtc);
        Assert.Null(reopened.AcknowledgedByUserId);
    }

    [Fact]
    public void Alert_Acknowledge_rejects_blank_actor_user_id()
    {
        var a = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        Assert.Throws<ArgumentException>(() => a.Acknowledge(DateTime.UtcNow, "  "));
    }

    [Fact]
    public void Alert_Resolve_rejects_blank_actor_user_id()
    {
        var a = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        Assert.Throws<ArgumentException>(() => a.Resolve(DateTime.UtcNow, ""));
    }

    [Fact]
    public void Alert_Reopen_rejects_blank_actor_user_id()
    {
        var a = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow).Resolve(DateTime.UtcNow, "r");
        Assert.Throws<ArgumentException>(() => a.Reopen(DateTime.UtcNow, "\t"));
    }

    [Fact]
    public void Alert_ack_attribution_survives_resolve_and_reopen()
    {
        var ackAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var resAt = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var reopenAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var path = new Alert(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow)
            .Acknowledge(ackAt, "persistent-ack")
            .Resolve(resAt, "resolver")
            .Reopen(reopenAt, "reopener");

        Assert.Equal("persistent-ack", path.AcknowledgedByUserId);
        Assert.Null(path.ResolvedByUserId);
        Assert.Equal("reopener", path.ReopenedByUserId);
    }
}
