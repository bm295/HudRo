using DataStructures.Application.Models;
using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Application.UseCases;

public sealed class PaymentApplicationService(
  IFnbReadPort readPort,
  IOrderPort orderPort,
  IPaymentPort paymentPort)
{
  public async Task<PaymentResult> ChargeOrderAsync(Order order, PaymentMethod method, CancellationToken cancellationToken = default)
  {
    var menu = await readPort.GetMenuAsync(cancellationToken);
    var total = CalculateTotal(order, menu);

    var paymentReference = await paymentPort.ChargeAsync(order.Id, total, method, cancellationToken);
    order.MarkPaid();
    await orderPort.SaveAsync(order, cancellationToken);

    return new PaymentResult(order.Id, total, method, paymentReference);
  }

  private static decimal CalculateTotal(Order order, IReadOnlyDictionary<string, MenuItem> menu)
  {
    return order.Items.Sum(item => menu[item.Key].Price * item.Value);
  }
}
