using SignalForge.Application;
using SignalForge.Application.Queries;
using SignalForge.Application.UseCases;
using SignalForge.Contracts;
using SignalForge.Contracts.Rules;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class CreateRuleValidationTests
{
    [Fact]
    public async Task HandleAsync_rejects_invalid_numeric_MatchValue_for_SignalValueGreaterThan()
    {
        var handler = new CreateRule.Handler(new FakeRuleRepository(), new FakeRuleAuditRepository());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new CreateRuleRequest("Rule", RuleTypes.SignalValueGreaterThan, "abc", true),
            CancellationToken.None));

        Assert.Contains("valid number", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_rejects_blank_MatchValue()
    {
        var handler = new CreateRule.Handler(new FakeRuleRepository(), new FakeRuleAuditRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new CreateRuleRequest("Rule", RuleTypes.SignalTypeEquals, "   ", true),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_accepts_valid_numeric_MatchValue_for_SignalValueGreaterThan()
    {
        var repo = new FakeRuleRepository();
        var audit = new FakeRuleAuditRepository();
        var handler = new CreateRule.Handler(repo, audit);

        var response = await handler.HandleAsync(
            new CreateRuleRequest("Threshold", RuleTypes.SignalValueGreaterThan, "10.5", true),
            CancellationToken.None);

        Assert.Equal(RuleTypes.SignalValueGreaterThan, response.RuleType);
        Assert.Equal("10.5", response.MatchValue);
        Assert.Single(repo.Added);
        var created = Assert.Single(audit.Entries);
        Assert.Equal(RuleAuditActions.Created, created.Action);
        Assert.Equal(response.Id, created.RuleId);
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

    private sealed class FakeRuleRepository : IRuleRepository
    {
        public List<Rule> Added { get; } = [];

        public Task AddAsync(Rule rule, CancellationToken cancellationToken)
        {
            Added.Add(rule);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Rule>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult((IReadOnlyList<Rule>)Array.Empty<Rule>());

        public Task<PagedResult<Rule>> ListPagedAsync(RuleListQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<Rule>([], query.Page, query.PageSize, 0));

        public Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Added.FirstOrDefault(r => r.Id == id));

        public Task UpdateAsync(Rule rule, CancellationToken cancellationToken)
        {
            var ix = Added.FindIndex(r => r.Id == rule.Id);
            if (ix >= 0)
                Added[ix] = rule;
            return Task.CompletedTask;
        }
    }
}
