using DataStructures.Application.Models;
using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Application.UseCases;

public sealed class PaymentApplicationService(
  IFnbReadPort readPort,
  IOrderPort orderPort,
  IPaymentPort paymentPort,
  InventoryApplicationService inventoryService)
{
  public async Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default)
  {
    var order = await LoadOrderAsync(command.OrderId, cancellationToken);
    var menu = await readPort.GetMenuAsync(cancellationToken);

    var total = CalculateTotal(order, menu);

    await inventoryService.EnsureAvailableAsync(order, cancellationToken);
    await inventoryService.DeductAsync(order, cancellationToken);

    var paymentReference = await paymentPort.ChargeAsync(order.Id, total, command.Method, cancellationToken);
    order.MarkPaid();
    await orderPort.SaveAsync(order, cancellationToken);

    return new PaymentResult(order.Id, total, command.Method, paymentReference);
  }

  private async Task<Order> LoadOrderAsync(Guid orderId, CancellationToken cancellationToken)
  {
    return await orderPort.FindByIdAsync(orderId, cancellationToken)
        ?? throw new KeyNotFoundException($"Order not found: {orderId}");
  }

  private static decimal CalculateTotal(Order order, IReadOnlyDictionary<string, MenuItem> menu)
  {
    return order.Items.Sum(item => menu[item.Key].Price * item.Value);
  }
}
