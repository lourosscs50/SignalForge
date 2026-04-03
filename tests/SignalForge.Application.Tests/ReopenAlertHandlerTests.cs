using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class ReopenAlertHandlerTests
{
    private const string ActorId = "650e8400-e29b-41d4-a716-446655440000";

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
    public async Task HandleAsync_reopens_resolved_alert_and_persists_once()
    {
        var resAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var reopenAt = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var alert = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow).Resolve(resAt, "resolver");
        var repos = new FakeAlertRepository { Stored = alert };
        var handler = new ReopenAlert.Handler(repos, new FixedClock(reopenAt), new FixedCurrentUser(ActorId));

        var result = await handler.HandleAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.IsResolved);
        Assert.Null(result.ResolvedAtUtc);
        Assert.Null(result.ResolvedByUserId);
        Assert.Equal(new DateTimeOffset(reopenAt, TimeSpan.Zero), result.ReopenedAtUtc);
        Assert.Equal(ActorId, result.ReopenedByUserId);
        Assert.Equal(1, repos.UpdateCount);
        Assert.NotNull(repos.Stored);
        Assert.False(repos.Stored!.IsResolved);
        Assert.Null(repos.Stored.ResolvedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_returns_null_when_alert_missing()
    {
        var repos = new FakeAlertRepository();
        var handler = new ReopenAlert.Handler(repos, new FixedClock(DateTime.UtcNow), new NullCurrentUser());

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, repos.UpdateCount);
    }

    [Fact]
    public async Task HandleAsync_throws_when_actor_missing_and_alert_exists()
    {
        var id = Guid.NewGuid();
        var resolved = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow).Resolve(DateTime.UtcNow, "x");
        var repos = new FakeAlertRepository { Stored = resolved };
        var handler = new ReopenAlert.Handler(repos, new FixedClock(DateTime.UtcNow), new NullCurrentUser());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(id, CancellationToken.None));
        Assert.Equal(0, repos.UpdateCount);
    }

    [Fact]
    public async Task HandleAsync_does_not_update_when_already_unresolved()
    {
        var id = Guid.NewGuid();
        var alert = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var repos = new FakeAlertRepository { Stored = alert };
        var handler = new ReopenAlert.Handler(repos, new FixedClock(DateTime.UtcNow), new FixedCurrentUser(ActorId));

        var result = await handler.HandleAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.IsResolved);
        Assert.Null(result.ResolvedAtUtc);
        Assert.Equal(0, repos.UpdateCount);
    }

    [Fact]
    public async Task HandleAsync_second_reopen_idempotent_no_extra_update()
    {
        var resAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var reopenAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var resolved = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow).Resolve(resAt, "r");
        var repos = new FakeAlertRepository { Stored = resolved };
        var handler = new ReopenAlert.Handler(repos, new FixedClock(reopenAt), new FixedCurrentUser(ActorId));

        var first = await handler.HandleAsync(id, CancellationToken.None);
        Assert.NotNull(first);
        Assert.False(first.IsResolved);
        Assert.Equal(1, repos.UpdateCount);

        var second = await handler.HandleAsync(id, CancellationToken.None);
        Assert.NotNull(second);
        Assert.False(second.IsResolved);
        Assert.Equal(new DateTimeOffset(reopenAt, TimeSpan.Zero), second.ReopenedAtUtc);
        Assert.Equal(ActorId, second.ReopenedByUserId);
        Assert.Equal(1, repos.UpdateCount);
    }

    [Fact]
    public async Task HandleAsync_preserves_acknowledgment_and_ack_attribution_after_reopen()
    {
        var ackAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var resAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var reopenAt = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var resolved = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow).Acknowledge(ackAt, "acker").Resolve(resAt, "res");
        var repos = new FakeAlertRepository { Stored = resolved };
        var handler = new ReopenAlert.Handler(repos, new FixedClock(reopenAt), new FixedCurrentUser(ActorId));

        var result = await handler.HandleAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsAcknowledged);
        Assert.Equal("acker", result.AcknowledgedByUserId);
        Assert.Equal(new DateTimeOffset(ackAt, TimeSpan.Zero), result.AcknowledgedAtUtc);
        Assert.False(result.IsResolved);
        Assert.Null(result.ResolvedAtUtc);
        Assert.Null(result.ResolvedByUserId);
        Assert.Equal(ActorId, result.ReopenedByUserId);
    }

    [Fact]
    public async Task HandleAsync_clears_resolved_attribution_in_response_after_reopen()
    {
        var id = Guid.NewGuid();
        var resolved = new Alert(id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow).Resolve(DateTime.UtcNow, "was-resolver");
        var repos = new FakeAlertRepository { Stored = resolved };
        var handler = new ReopenAlert.Handler(repos, new FixedClock(DateTime.UtcNow), new FixedCurrentUser(ActorId));

        var result = await handler.HandleAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.ResolvedByUserId);
    }
}
