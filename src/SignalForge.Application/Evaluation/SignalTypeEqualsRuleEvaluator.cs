using SignalForge.Application;
using SignalForge.Domain;

namespace SignalForge.Application.Evaluation;

public sealed class SignalTypeEqualsRuleEvaluator : IRuleEvaluator
{
    public bool CanEvaluate(Rule rule) =>
        string.Equals(rule.RuleType, RuleTypes.SignalTypeEquals, StringComparison.OrdinalIgnoreCase);

    public bool IsMatch(Rule rule, Signal signal) =>
        string.Equals(rule.MatchValue, signal.Type, StringComparison.OrdinalIgnoreCase);
}
