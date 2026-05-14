using DataStructures.Application.Models;
using DataStructures.Application.Inventory;
using DataStructures.Application.Payment;
using DataStructures.Domain;

namespace DataStructures.Application.Workflows;

public sealed class CheckoutOrderWorkflow(
  OrderApplicationService orderService,
  InventoryApplicationService inventoryService,
  PaymentApplicationService paymentService)
{
  public async Task<CloseOrderResult> ExecuteAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default)
  {
    var order = await orderService.LoadOrderAsync(command.OrderId, cancellationToken);

    await inventoryService.EnsureAvailableAsync(order, cancellationToken);
    await inventoryService.DeductAsync(order, cancellationToken);

    var paymentResult = await paymentService.ChargeOrderAsync(order, command.Method, cancellationToken);

    return await orderService.CloseOrderStateOnlyAsync(order.Id, paymentResult, cancellationToken);
  }
}
