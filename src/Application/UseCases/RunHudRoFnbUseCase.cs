using DataStructures.Application.Models;

namespace DataStructures.Application.UseCases;

public sealed class RunHudRoFnbUseCase(
  OrderApplicationService orderService,
  PaymentApplicationService paymentService,
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

  public Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default)
    => paymentService.ProcessPaymentAsync(command, cancellationToken);

  public Task<CloseOrderResult> CloseOrderAsync(Guid orderId, PaymentResult paymentResult, CancellationToken cancellationToken = default)
    => orderService.CloseOrderStateOnlyAsync(orderId, paymentResult, cancellationToken);

  public Task<ServiceSummaryResult> BuildDailySummaryAsync(BuildServiceSummaryQuery query, CancellationToken cancellationToken = default)
    => reportingService.BuildDailySummaryAsync(query, cancellationToken);
}
