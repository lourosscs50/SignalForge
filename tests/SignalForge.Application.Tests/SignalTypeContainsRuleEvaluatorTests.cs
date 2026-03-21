using SignalForge.Application.Evaluation;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class SignalTypeContainsRuleEvaluatorTests
{
    private readonly SignalTypeContainsRuleEvaluator _sut = new();

    [Fact]
    public void IsMatch_returns_true_when_signal_type_contains_match_value()
    {
        var rule = new Rule(Guid.NewGuid(), "n", RuleTypes.SignalTypeContains, "temp", true, DateTime.UtcNow);
        var signal = new Signal(Guid.NewGuid(), "src", "temperature", "", null, DateTime.UtcNow, DateTime.UtcNow);

        Assert.True(_sut.CanEvaluate(rule));
        Assert.True(_sut.IsMatch(rule, signal));
    }

    [Fact]
    public void IsMatch_returns_false_when_signal_type_does_not_contain_match_value()
    {
        var rule = new Rule(Guid.NewGuid(), "n", RuleTypes.SignalTypeContains, "temp", true, DateTime.UtcNow);
        var signal = new Signal(Guid.NewGuid(), "src", "pressure", "", null, DateTime.UtcNow, DateTime.UtcNow);

        Assert.True(_sut.CanEvaluate(rule));
        Assert.False(_sut.IsMatch(rule, signal));
    }

    [Fact]
    public void IsMatch_is_case_insensitive_for_contains()
    {
        var rule = new Rule(Guid.NewGuid(), "n", RuleTypes.SignalTypeContains, "TEMP", true, DateTime.UtcNow);
        var signal = new Signal(Guid.NewGuid(), "src", "preTEMPsure", "", null, DateTime.UtcNow, DateTime.UtcNow);

        Assert.True(_sut.CanEvaluate(rule));
        Assert.True(_sut.IsMatch(rule, signal));
    }
}
