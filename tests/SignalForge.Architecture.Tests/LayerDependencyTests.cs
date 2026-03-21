using NetArchTest.Rules;
using SignalForge.Application;
using SignalForge.Contracts.Rules;
using SignalForge.Domain;
using SignalForge.Infrastructure.Persistence;

namespace SignalForge.Architecture.Tests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_does_not_depend_on_Application_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(typeof(Rule).Assembly)
            .That()
            .ResideInNamespaceStartingWith("SignalForge.Domain")
            .ShouldNot()
            .HaveDependencyOnAny(
                "SignalForge.Application",
                "SignalForge.Infrastructure",
                "SignalForge.Api")
            .GetResult();

        AssertArch(result);
    }

    [Fact]
    public void Application_does_not_depend_on_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(typeof(IRuleRepository).Assembly)
            .That()
            .ResideInNamespaceStartingWith("SignalForge.Application")
            .ShouldNot()
            .HaveDependencyOnAny(
                "SignalForge.Infrastructure",
                "SignalForge.Api")
            .GetResult();

        AssertArch(result);
    }

    [Fact]
    public void Contracts_do_not_depend_on_Api_or_Infrastructure()
    {
        var result = Types.InAssembly(typeof(CreateRuleRequest).Assembly)
            .That()
            .ResideInNamespaceStartingWith("SignalForge.Contracts")
            .ShouldNot()
            .HaveDependencyOnAny(
                "SignalForge.Api",
                "SignalForge.Infrastructure")
            .GetResult();

        AssertArch(result);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_Api()
    {
        var result = Types.InAssembly(typeof(EfRuleRepository).Assembly)
            .That()
            .ResideInNamespaceStartingWith("SignalForge.Infrastructure")
            .ShouldNot()
            .HaveDependencyOn("SignalForge.Api")
            .GetResult();

        AssertArch(result);
    }

    private static void AssertArch(TestResult result)
    {
        Assert.True(
            result.IsSuccessful,
            result.IsSuccessful ? string.Empty : string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
