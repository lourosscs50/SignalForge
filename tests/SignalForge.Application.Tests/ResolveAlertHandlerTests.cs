using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class ResolveAlertHandlerTests
{
    private const string ActorId = "550e8400-e29b-41d4-a716-446655440000";

    private sealed class FixedClock(DateTime utc) : IDateTimeProvider
    {
        public DateTime UtcNow => utc;
    }

    private sealed class FixedCurrentUser(string userId) : ICurrentUser
    {
        public string? UserId => userId;
    }

    private sealed class NullCurrentUser : ICurrentUser
    {
        public string? UserId => null;
    }

    private sealed class FakeAlertRepository : IAlertRepository
    {
        public Alert? Stored { get; set; }
        public int UpdateCount { get; private set; }

        public Task AddAsync(Alert alert, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Stored?.Id == id ? Stored : null);

        public Task UpdateAsync(Alert alert, CancellationToken cancellationToken)
        {
            UpdateCount++;
            Stored = alert;
            return Task.CompletedTask;
        }

        public Task<PagedResult<Alert>> ListPagedAsync(AlertListQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<Alert>([], query.Page, query.PageSize, 0));

        public Task<IReadOnlyList<Alert>> ListAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Alert>>(Stored is null ? [] : [Stored]);

        public Task<IReadOnlyList<Alert>> ListByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Alert>>(Stored?.RuleId == ruleId ? [Stored!] : []);
    }

    [Fact]
    public async Task HandleAsync_resolves_unresolved_alert_and_persists_once()
    {
        var t0 = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var alert = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var repos = new FakeAlertRepository { Stored = alert };
        var clock = new FixedClock(t0);
        var handler = new ResolveAlert.Handler(repos, clock, new FixedCurrentUser(ActorId));

        var result = await handler.HandleAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsResolved);
        Assert.Equal(new DateTimeOffset(t0, TimeSpan.Zero), result.ResolvedAtUtc);
        Assert.Equal(ActorId, result.ResolvedByUserId);
        Assert.False(result.IsAcknowledged);
        Assert.Equal(1, repos.UpdateCount);
        Assert.True(repos.Stored!.IsResolved);
        Assert.Equal(t0, repos.Stored.ResolvedAtUtc);
        Assert.Equal(ActorId, repos.Stored.ResolvedByUserId);
    }

    [Fact]
    public async Task HandleAsync_returns_null_when_alert_missing_without_requiring_actor()
    {
        var repos = new FakeAlertRepository();
        var handler = new ResolveAlert.Handler(repos, new FixedClock(DateTime.UtcNow), new NullCurrentUser());

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, repos.UpdateCount);
    }

    [Fact]
    public async Task HandleAsync_throws_when_actor_missing_and_alert_exists()
    {
        var id = Guid.NewGuid();
        var repos = new FakeAlertRepository { Stored = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow) };
        var handler = new ResolveAlert.Handler(repos, new FixedClock(DateTime.UtcNow), new NullCurrentUser());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(id, CancellationToken.None));
        Assert.Equal(0, repos.UpdateCount);
    }

    [Fact]
    public async Task HandleAsync_does_not_update_when_already_resolved()
    {
        var resAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var alert = new Alert(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            IsAcknowledged: true,
            AcknowledgedAtUtc: DateTime.UtcNow,
            AcknowledgedByUserId: "ack",
            IsResolved: true,
            ResolvedAtUtc: resAt,
            ResolvedByUserId: "first-resolver");
        var repos = new FakeAlertRepository { Stored = alert };
        var handler = new ResolveAlert.Handler(repos, new FixedClock(DateTime.UtcNow), new FixedCurrentUser("other"));

        var result = await handler.HandleAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsResolved);
        Assert.Equal(new DateTimeOffset(resAt, TimeSpan.Zero), result.ResolvedAtUtc);
        Assert.Equal("first-resolver", result.ResolvedByUserId);
        Assert.Equal(0, repos.UpdateCount);
    }

    [Fact]
    public async Task HandleAsync_second_resolve_idempotent_same_ResolvedAtUtc_and_ResolvedByUserId()
    {
        var tResolve = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var alert = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var repos = new FakeAlertRepository { Stored = alert };
        var handler = new ResolveAlert.Handler(repos, new FixedClock(tResolve), new FixedCurrentUser(ActorId));

        var first = await handler.HandleAsync(id, CancellationToken.None);
        Assert.NotNull(first);
        var resTs = first.ResolvedAtUtc!.Value;
        var resBy = first.ResolvedByUserId;
        Assert.Equal(1, repos.UpdateCount);

        var laterClock = new FixedClock(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        var handler2 = new ResolveAlert.Handler(repos, laterClock, new FixedCurrentUser("someone-else"));
        var second = await handler2.HandleAsync(id, CancellationToken.None);

        Assert.NotNull(second);
        Assert.Equal(resTs, second.ResolvedAtUtc);
        Assert.Equal(resBy, second.ResolvedByUserId);
        Assert.Equal(1, repos.UpdateCount);
    }

    [Fact]
    public async Task HandleAsync_preserves_acknowledgment_when_resolving_after_ack()
    {
        var ackAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var resAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var acked = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow).Acknowledge(ackAt, "acker");
        var repos = new FakeAlertRepository { Stored = acked };
        var handler = new ResolveAlert.Handler(repos, new FixedClock(resAt), new FixedCurrentUser(ActorId));

        var result = await handler.HandleAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsResolved);
        Assert.True(result.IsAcknowledged);
        Assert.Equal("acker", result.AcknowledgedByUserId);
        Assert.Equal(new DateTimeOffset(ackAt, TimeSpan.Zero), result.AcknowledgedAtUtc);
        Assert.Equal(new DateTimeOffset(resAt, TimeSpan.Zero), result.ResolvedAtUtc);
        Assert.Equal(ActorId, result.ResolvedByUserId);
    }
}
