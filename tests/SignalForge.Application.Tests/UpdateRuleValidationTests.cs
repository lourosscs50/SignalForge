using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts;
using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class UpdateRuleValidationTests
{
    [Fact]
    public async Task G_HandleAsync_rejects_empty_Name()
    {
        var id = Guid.NewGuid();
        var rule = new Rule(id, "n", RuleTypes.SignalTypeEquals, "x", true, false, DateTime.UtcNow);
        var handler = new UpdateRule.Handler(
            new FakeRuleRepository(rule),
            new FakeRuleAuditRepository(),
            new FixedClock(DateTime.UtcNow));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            id,
            new UpdateRuleRequest("   ", "y"),
            CancellationToken.None));
    }

    [Fact]
    public async Task H_HandleAsync_rejects_empty_MatchValue()
    {
        var id = Guid.NewGuid();
        var rule = new Rule(id, "n", RuleTypes.SignalTypeEquals, "x", true, false, DateTime.UtcNow);
        var handler = new UpdateRule.Handler(
            new FakeRuleRepository(rule),
            new FakeRuleAuditRepository(),
            new FixedClock(DateTime.UtcNow));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            id,
            new UpdateRuleRequest("Name", "  "),
            CancellationToken.None));
    }

    [Fact]
    public async Task I_HandleAsync_rejects_invalid_numeric_MatchValue_for_SignalValueGreaterThan()
    {
        var id = Guid.NewGuid();
        var rule = new Rule(id, "n", RuleTypes.SignalValueGreaterThan, "10", true, false, DateTime.UtcNow);
        var handler = new UpdateRule.Handler(
            new FakeRuleRepository(rule),
            new FakeRuleAuditRepository(),
            new FixedClock(DateTime.UtcNow));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            id,
            new UpdateRuleRequest("Name", "not-a-number"),
            CancellationToken.None));

        Assert.Contains("valid number", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task J_HandleAsync_accepts_valid_numeric_MatchValue_for_SignalValueGreaterThan()
    {
        var id = Guid.NewGuid();
        var rule = new Rule(id, "n", RuleTypes.SignalValueGreaterThan, "10", true, false, DateTime.UtcNow);
        var audit = new FakeRuleAuditRepository();
        var handler = new UpdateRule.Handler(
            new FakeRuleRepository(rule),
            audit,
            new FixedClock(DateTime.UtcNow));

        var response = await handler.HandleAsync(
            id,
            new UpdateRuleRequest("Name", "  42.5  "),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("42.5", response.MatchValue);
        var updated = Assert.Single(audit.Entries);
        Assert.Equal(RuleAuditActions.Updated, updated.Action);
    }

    private sealed class FixedClock(DateTime utc) : IDateTimeProvider
    {
        public DateTime UtcNow => utc;
    }

    private sealed class FakeRuleRepository(Rule rule) : IRuleRepository
    {
        public Task AddAsync(Rule r, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Rule?>(id == rule.Id ? rule : null);

        public Task UpdateAsync(Rule r, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<Rule>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<Rule>)Array.Empty<Rule>());

        public Task<PagedResult<Rule>> ListPagedAsync(RuleListQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<Rule>([], query.Page, query.PageSize, 0));
    }

    private sealed class FakeRuleAuditRepository : IRuleAuditRepository
    {
        public List<RuleAuditEntry> Entries { get; } = [];

        public Task AddAsync(RuleAuditEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RuleAuditEntry>> ListByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<RuleAuditEntry>)Entries.Where(e => e.RuleId == ruleId).ToList());
    }
}
