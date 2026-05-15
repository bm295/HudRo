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
      .HaveDependencyOnAny(
        "DataStructures.Helpers",
        "DataStructures.Extensions",
        "DataStructures.BackgroundServices")
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
      .HaveDependencyOnAny(
        "DataStructures.Helpers",
        "DataStructures.Extensions",
        "DataStructures.BackgroundServices")
      .GetResult();

    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames));
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
}
