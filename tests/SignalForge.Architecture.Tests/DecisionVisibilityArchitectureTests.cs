using NetArchTest.Rules;
using SignalForge.Contracts.Decisions;

namespace SignalForge.Architecture.Tests;

public sealed class DecisionVisibilityArchitectureTests
{
    [Fact]
    public void Contracts_Decisions_does_not_depend_on_Domain_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(typeof(DecisionVisibilityResponse).Assembly)
            .That()
            .ResideInNamespaceStartingWith("SignalForge.Contracts.Decisions")
            .ShouldNot()
            .HaveDependencyOnAny(
                "SignalForge.Domain",
                "SignalForge.Infrastructure",
                "SignalForge.Api")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            result.IsSuccessful ? string.Empty : string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_Decisions_does_not_depend_on_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(typeof(SignalForge.Application.Decisions.DecisionObservationRecorder).Assembly)
            .That()
            .ResideInNamespaceStartingWith("SignalForge.Application.Decisions")
            .ShouldNot()
            .HaveDependencyOnAny(
                "SignalForge.Infrastructure",
                "SignalForge.Api")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            result.IsSuccessful ? string.Empty : string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
