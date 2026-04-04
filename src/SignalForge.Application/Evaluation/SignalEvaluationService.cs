using SignalForge.Application;
using SignalForge.Application.Automation;
using SignalForge.Application.Decisions;
using SignalForge.Contracts.Automation;
using SignalForge.Domain;

namespace SignalForge.Application.Evaluation;

public sealed class SignalEvaluationService(
    IRuleRepository rules,
    IAlertRepository alerts,
    IDateTimeProvider clock,
    IEnumerable<IRuleEvaluator> evaluators,
    AlertLifecycleAutomationCoordinator lifecycleAutomation) : ISignalEvaluationService
{
    public async Task EvaluateAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        var activeRules = await rules.ListActiveAsync(cancellationToken);

        foreach (var rule in activeRules)
        {
            var evaluator = evaluators.FirstOrDefault(e => e.CanEvaluate(rule));
            if (evaluator is null)
                continue;

            if (!evaluator.IsMatch(rule, signal))
                continue;

            var utcNow = clock.UtcNow;
            var alert = new Alert(
                Id: Guid.NewGuid(),
                SignalId: signal.Id,
                RuleId: rule.Id,
                CreatedAtUtc: utcNow);

            await alerts.AddAsync(alert, cancellationToken);
            var observation = new DecisionObservationContext(
                EvaluatorStrategyKey: evaluator.GetType().Name,
                InputSummary: SignalObservationSummaryFormatter.Format(signal));
            await lifecycleAutomation.NotifyRealTransitionAsync(
                AlertLifecycleTransitionType.AlertCreated,
                alert,
                rule,
                utcNow,
                cancellationToken,
                observation);
        }
    }
}
