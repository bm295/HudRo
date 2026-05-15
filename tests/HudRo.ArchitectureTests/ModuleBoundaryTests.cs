using DataStructures.Application.Inventory;
using DataStructures.Application.Loyalty;
using DataStructures.Application.Order;
using DataStructures.Application.Payment;
using DataStructures.Application.Ports;
using DataStructures.Application.Workflows;
using DataStructures.Domain;
using NetArchTest.Rules;
using Xunit;

namespace HudRo.ArchitectureTests;

public sealed class ModuleBoundaryTests
{
  private static readonly string[] ForbiddenUtilityNamespaces =
  [
    "DataStructures.Helpers",
    "DataStructures.Extensions",
    "DataStructures.BackgroundServices"
  ];

  private static readonly string[] CoreDomainVerbs =
  [
    "Authorize",
    "Capture",
    "Redeem",
    "Reserve"
  ];

  [Fact]
  public void OrderNamespace_ShouldNotDependOn_IPaymentGatewayPort()
  {
    var result = Types.InAssembly(typeof(OrderApplicationService).Assembly)
      .That()
      .ResideInNamespace("DataStructures.Application.Order", true)
      .ShouldNot()
      .HaveDependencyOn(typeof(IPaymentGatewayPort).FullName!)
      .GetResult();

    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames));
  }

  [Fact]
  public void WorkflowNamespace_ShouldNotDependOn_HelperExtensionOrBackgroundNamespaces()
  {
    var result = Types.InAssembly(typeof(CheckoutOrderWorkflow).Assembly)
      .That()
      .ResideInNamespace("DataStructures.Application", true)
      .ShouldNot()
      .HaveDependencyOnAny(ForbiddenUtilityNamespaces)
      .GetResult();

    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames));
  }

  [Fact]
  public void DomainNamespace_ShouldNotDependOn_HelperExtensionOrBackgroundNamespaces()
  {
    var result = Types.InAssembly(typeof(Order).Assembly)
      .That()
      .ResideInNamespace("DataStructures.Domain", true)
      .ShouldNot()
      .HaveDependencyOnAny(ForbiddenUtilityNamespaces)
      .GetResult();

    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames));
  }

  [Fact]
  public void PaymentAndLoyaltyNamespaces_ShouldNotDependOn_HelperExtensionOrBackgroundNamespaces()
  {
    var result = Types.InAssembly(typeof(Order).Assembly)
      .That()
      .ResideInNamespace("DataStructures.Domain.Payments", true)
      .Or()
      .ResideInNamespace("DataStructures.Domain.Loyalty", true)
      .ShouldNot()
      .HaveDependencyOnAny(ForbiddenUtilityNamespaces)
      .GetResult();

    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames));
  }

  [Fact]
  public void PaymentAndLoyaltyCoreVerbs_ShouldNotAppearInUtilityNamespaces()
  {
    var forbiddenMatches = typeof(Order).Assembly
      .GetTypes()
      .Where(type => type.Namespace is not null)
      .Where(type => type.Namespace.StartsWith("DataStructures.Helpers")
        || type.Namespace.StartsWith("DataStructures.Extensions")
        || type.Namespace.StartsWith("DataStructures.BackgroundServices"))
      .SelectMany(type => type.GetMethods(System.Reflection.BindingFlags.Instance
                                           | System.Reflection.BindingFlags.Static
                                           | System.Reflection.BindingFlags.Public
                                           | System.Reflection.BindingFlags.NonPublic)
        .Where(method => CoreDomainVerbs.Any(verb => method.Name.Contains(verb, StringComparison.OrdinalIgnoreCase)))
        .Select(method => $"{type.FullName}.{method.Name}"))
      .OrderBy(x => x)
      .ToArray();

    Assert.True(forbiddenMatches.Length == 0, $"Forbidden core-domain verb methods found in utility namespaces: {string.Join(", ", forbiddenMatches)}");
  }

  [Fact]
  public void CheckoutWorkflow_ShouldDependOnlyOnApplicationServices()
  {
    var result = Types.InAssembly(typeof(CheckoutOrderWorkflow).Assembly)
      .That()
      .Are(typeof(CheckoutOrderWorkflow))
      .ShouldNot()
      .HaveDependencyOnAny(
        typeof(IInventoryPort).FullName!,
        typeof(IPaymentGatewayPort).FullName!,
        typeof(ILoyaltyAccountPort).FullName!,
        typeof(IOrderPort).FullName!)
      .GetResult();

    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames));
  }

  [Fact]
  public void Workflows_ShouldNotDependOnInfrastructureAdapters()
  {
    var result = Types.InAssembly(typeof(CheckoutOrderWorkflow).Assembly)
      .That()
      .ResideInNamespace("DataStructures.Application.Workflows", true)
      .ShouldNot()
      .HaveDependencyOnAny(
        "DataStructures.Infrastructure",
        "DataStructures.Infrastructure.",
        "DataStructures.Adapters")
      .GetResult();

    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames));
  }
}
