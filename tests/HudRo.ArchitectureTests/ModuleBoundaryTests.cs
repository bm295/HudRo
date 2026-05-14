using Xunit;
using DataStructures.Application.Order;
using DataStructures.Application.Ports;
using DataStructures.Application.Reporting;
using NetArchTest.Rules;

namespace HudRo.ArchitectureTests;

public sealed class ModuleBoundaryTests
{
  [Fact]
  public void OrderNamespace_ShouldNotDependOn_IPaymentPort()
  {
    var result = Types.InAssembly(typeof(OrderApplicationService).Assembly)
      .That()
      .ResideInNamespace("DataStructures.Application.Order", true)
      .ShouldNot()
      .HaveDependencyOn(typeof(IPaymentPort).FullName!)
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
        typeof(IPaymentPort).FullName!)
      .GetResult();

    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames));
  }
}
