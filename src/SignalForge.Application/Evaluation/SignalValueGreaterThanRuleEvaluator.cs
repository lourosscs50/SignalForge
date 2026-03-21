using System.Globalization;
using SignalForge.Application;
using SignalForge.Domain;

namespace SignalForge.Application.Evaluation;

public sealed class SignalValueGreaterThanRuleEvaluator : IRuleEvaluator
{
    public bool CanEvaluate(Rule rule) =>
        string.Equals(rule.RuleType, RuleTypes.SignalValueGreaterThan, StringComparison.OrdinalIgnoreCase);

    public bool IsMatch(Rule rule, Signal signal)
    {
        if (signal.Value is null)
            return false;

        if (!double.TryParse(rule.MatchValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold))
            return false;

        return signal.Value.Value > threshold;
    }
}
