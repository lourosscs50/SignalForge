using SignalForge.Application.Evaluation;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class SignalValueGreaterThanRuleEvaluatorTests
{
    private readonly SignalValueGreaterThanRuleEvaluator _sut = new();

    [Fact]
    public void IsMatch_returns_true_when_signal_value_is_greater_than_threshold()
    {
        var rule = new Rule(Guid.NewGuid(), "n", RuleTypes.SignalValueGreaterThan, "10", true, DateTime.UtcNow);
        var signal = new Signal(Guid.NewGuid(), "src", "t", "", 10.5, DateTime.UtcNow, DateTime.UtcNow);

        Assert.True(_sut.CanEvaluate(rule));
        Assert.True(_sut.IsMatch(rule, signal));
    }

    [Fact]
    public void IsMatch_returns_false_when_signal_value_is_less_than_or_equal_to_threshold()
    {
        var rule = new Rule(Guid.NewGuid(), "n", RuleTypes.SignalValueGreaterThan, "10", true, DateTime.UtcNow);
        var below = new Signal(Guid.NewGuid(), "src", "t", "", 9.9, DateTime.UtcNow, DateTime.UtcNow);
        var equal = new Signal(Guid.NewGuid(), "src", "t", "", 10.0, DateTime.UtcNow, DateTime.UtcNow);

        Assert.True(_sut.CanEvaluate(rule));
        Assert.False(_sut.IsMatch(rule, below));
        Assert.False(_sut.IsMatch(rule, equal));
    }

    [Fact]
    public void IsMatch_returns_false_when_signal_value_is_null()
    {
        var rule = new Rule(Guid.NewGuid(), "n", RuleTypes.SignalValueGreaterThan, "10", true, DateTime.UtcNow);
        var signal = new Signal(Guid.NewGuid(), "src", "t", "", null, DateTime.UtcNow, DateTime.UtcNow);

        Assert.True(_sut.CanEvaluate(rule));
        Assert.False(_sut.IsMatch(rule, signal));
    }
}
