using SignalForge.Application;
using SignalForge.Domain;

namespace SignalForge.Application.Evaluation;

public sealed class SignalEvaluationService(
    IRuleRepository rules,
    IAlertRepository alerts,
    IDateTimeProvider clock,
    IEnumerable<IRuleEvaluator> evaluators) : ISignalEvaluationService
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

            var alert = new Alert(
                Id: Guid.NewGuid(),
                SignalId: signal.Id,
                RuleId: rule.Id,
                CreatedAtUtc: clock.UtcNow);

            await alerts.AddAsync(alert, cancellationToken);
        }
    }
}
