using DataStructures.Application.Order;
using DataStructures.Application.Inventory;
using DataStructures.Application.Payment;
using DataStructures.Application.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace DataStructures.Application.DependencyInjection;

public static class ApplicationModuleServiceCollectionExtensions
{
  public static IServiceCollection AddOrderModule(this IServiceCollection services)
  {
    services.AddSingleton<OrderApplicationService>();
    services.AddSingleton<CheckoutOrderWorkflow>();

    return services;
  }

  public static IServiceCollection AddInventoryModule(this IServiceCollection services)
  {
    services.AddSingleton<InventoryApplicationService>();

    return services;
  }

  public static IServiceCollection AddPaymentModule(this IServiceCollection services)
  {
    services.AddSingleton<PaymentApplicationService>();

    return services;
  }

  public static IServiceCollection AddReportingModule(this IServiceCollection services)
  {
    services.AddSingleton<ReportingApplicationService>();

    return services;
  }
}
