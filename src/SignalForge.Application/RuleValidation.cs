using System.Globalization;
using SignalForge.Domain;

namespace SignalForge.Application;

/// <summary>Shared validation for rule name/match value input (create and update).</summary>
public static class RuleValidation
{
    public static string NormalizeRequiredName(string? name, string paramName)
    {
        var n = (name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(n))
            throw new ArgumentException("Name is required.", paramName);
        return n;
    }

    public static string NormalizeRequiredMatchValue(string? matchValue, string paramName)
    {
        var v = (matchValue ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(v))
            throw new ArgumentException("MatchValue is required.", paramName);
        return v;
    }

    public static void ValidateMatchValueForCanonicalRuleType(
        string canonicalRuleType,
        string matchValue,
        string paramName)
    {
        if (canonicalRuleType == RuleTypes.SignalValueGreaterThan)
        {
            if (!double.TryParse(matchValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                throw new ArgumentException(
                    "MatchValue must be a valid number for SignalValueGreaterThan rules.",
                    paramName);
        }
    }

    public static bool TryNormalizeRuleType(string ruleType, out string canonicalRuleType)
    {
        if (string.Equals(ruleType, RuleTypes.SignalTypeEquals, StringComparison.OrdinalIgnoreCase))
        {
            canonicalRuleType = RuleTypes.SignalTypeEquals;
            return true;
        }

        if (string.Equals(ruleType, RuleTypes.SignalTypeContains, StringComparison.OrdinalIgnoreCase))
        {
            canonicalRuleType = RuleTypes.SignalTypeContains;
            return true;
        }

        if (string.Equals(ruleType, RuleTypes.SignalValueGreaterThan, StringComparison.OrdinalIgnoreCase))
        {
            canonicalRuleType = RuleTypes.SignalValueGreaterThan;
            return true;
        }

        canonicalRuleType = string.Empty;
        return false;
    }
}
