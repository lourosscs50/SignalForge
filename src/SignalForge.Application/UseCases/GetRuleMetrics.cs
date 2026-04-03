using SignalForge.Application;
using SignalForge.Contracts.Rules;

namespace SignalForge.Application.UseCases;

public static class GetRuleMetrics
{
    public sealed class Handler(IRuleRepository rules, IAlertRepository alerts)
    {
        public async Task<RuleMetricsSummaryResponse?> HandleAsync(Guid ruleId, CancellationToken cancellationToken)
        {
            var rule = await rules.GetByIdAsync(ruleId, cancellationToken);
            if (rule is null)
                return null;

            var items = await alerts.ListByRuleIdAsync(ruleId, cancellationToken);
            return AlertMappings.ToRuleMetricsSummary(rule, items);
        }
    }
}
