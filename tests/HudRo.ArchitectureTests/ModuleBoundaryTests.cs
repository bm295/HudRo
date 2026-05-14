using Xunit;
using DataStructures.Application.Order;
using DataStructures.Application.Ports;
using DataStructures.Application.Reporting;
using DataStructures.Application.Workflows;
using DataStructures.Domain;
using NetArchTest.Rules;

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
  public void ReportingService_ShouldNotDependOnMutationPorts()
  {
    var result = Types.InAssembly(typeof(ReportingApplicationService).Assembly)
      .That()
      .Are(typeof(ReportingApplicationService))
      .ShouldNot()
      .HaveDependencyOnAny(
        typeof(IOrderPort).FullName!,
        typeof(IInventoryPort).FullName!,
        typeof(IPaymentGatewayPort).FullName!)
      .GetResult();

    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames));
  }

  [Fact]
  public void ApplicationNamespace_ShouldNotDependOn_ConcreteInfrastructure()
  {
    var result = Types.InAssembly(typeof(CheckoutOrderWorkflow).Assembly)
      .That()
      .ResideInNamespace("DataStructures.Application", true)
      .ShouldNot()
      .HaveDependencyOn("DataStructures.Infrastructure")
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
}
