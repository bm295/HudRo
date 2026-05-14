using DataStructures.Application.Models;
using DataStructures.Application.Order;
using DataStructures.Application.Reporting;
using DataStructures.Application.Workflows;

namespace DataStructures.Application;

public sealed class RunHudRoFnbUseCase(
  OrderApplicationService orderService,
  CheckoutOrderWorkflow checkoutOrderWorkflow,
  ReportingApplicationService reportingService)
{
  public Task<Guid> CreateOrderAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    => orderService.CreateOrderAsync(command, cancellationToken);

  public Task AddItemAsync(AddOrderItemCommand command, CancellationToken cancellationToken = default)
    => orderService.AddItemAsync(command, cancellationToken);

  public Task RemoveItemAsync(RemoveOrderItemCommand command, CancellationToken cancellationToken = default)
    => orderService.RemoveItemAsync(command, cancellationToken);

  public Task SendToKitchenAsync(Guid orderId, CancellationToken cancellationToken = default)
    => orderService.SendToKitchenAsync(orderId, cancellationToken);

  public Task<CloseOrderResult> CheckoutOrderAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default)
    => checkoutOrderWorkflow.ExecuteAsync(command, cancellationToken);

  public Task<ServiceSummaryResult> BuildDailySummaryAsync(BuildServiceSummaryQuery query, CancellationToken cancellationToken = default)
    => reportingService.BuildDailySummaryAsync(query, cancellationToken);
}
