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
        var handler = new CreateRule.Handler(new FakeRuleRepository());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new CreateRuleRequest("Rule", RuleTypes.SignalValueGreaterThan, "abc", true),
            CancellationToken.None));

        Assert.Contains("valid number", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_rejects_blank_MatchValue()
    {
        var handler = new CreateRule.Handler(new FakeRuleRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new CreateRuleRequest("Rule", RuleTypes.SignalTypeEquals, "   ", true),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_accepts_valid_numeric_MatchValue_for_SignalValueGreaterThan()
    {
        var repo = new FakeRuleRepository();
        var handler = new CreateRule.Handler(repo);

        var response = await handler.HandleAsync(
            new CreateRuleRequest("Threshold", RuleTypes.SignalValueGreaterThan, "10.5", true),
            CancellationToken.None);

        Assert.Equal(RuleTypes.SignalValueGreaterThan, response.RuleType);
        Assert.Equal("10.5", response.MatchValue);
        Assert.Single(repo.Added);
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
    }
}
