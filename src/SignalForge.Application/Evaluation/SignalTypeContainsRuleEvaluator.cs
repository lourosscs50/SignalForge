using SignalForge.Application;
using SignalForge.Domain;

namespace SignalForge.Application.Evaluation;

public sealed class SignalTypeContainsRuleEvaluator : IRuleEvaluator
{
    public bool CanEvaluate(Rule rule) =>
        string.Equals(rule.RuleType, RuleTypes.SignalTypeContains, StringComparison.OrdinalIgnoreCase);

    public bool IsMatch(Rule rule, Signal signal) =>
        signal.Type.IndexOf(rule.MatchValue, StringComparison.OrdinalIgnoreCase) >= 0;
}
