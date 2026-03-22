using SignalForge.Application.Evaluation;
using SignalForge.Domain;

namespace SignalForge.Application.Tests;

public sealed class SignalTypeEqualsRuleEvaluatorTests
{
    private readonly SignalTypeEqualsRuleEvaluator _sut = new();

    [Fact]
    public void IsMatch_returns_true_when_signal_type_equals_match_value()
    {
        var rule = new Rule(Guid.NewGuid(), "n", RuleTypes.SignalTypeEquals, "temperature", true, false, DateTime.UtcNow);
        var signal = new Signal(Guid.NewGuid(), "src", "temperature", "", null, DateTime.UtcNow, DateTime.UtcNow);

        Assert.True(_sut.CanEvaluate(rule));
        Assert.True(_sut.IsMatch(rule, signal));
    }

    [Fact]
    public void IsMatch_returns_false_when_values_differ()
    {
        var rule = new Rule(Guid.NewGuid(), "n", RuleTypes.SignalTypeEquals, "temperature", true, false, DateTime.UtcNow);
        var signal = new Signal(Guid.NewGuid(), "src", "pressure", "", null, DateTime.UtcNow, DateTime.UtcNow);

        Assert.True(_sut.CanEvaluate(rule));
        Assert.False(_sut.IsMatch(rule, signal));
    }

    [Fact]
    public void IsMatch_is_case_insensitive_for_signal_type()
    {
        var rule = new Rule(Guid.NewGuid(), "n", RuleTypes.SignalTypeEquals, "Temperature", true, false, DateTime.UtcNow);
        var signal = new Signal(Guid.NewGuid(), "src", "temperature", "", null, DateTime.UtcNow, DateTime.UtcNow);

        Assert.True(_sut.CanEvaluate(rule));
        Assert.True(_sut.IsMatch(rule, signal));
    }
}
