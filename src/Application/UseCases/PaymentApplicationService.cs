using DataStructures.Application.Models;
using DataStructures.Application.Ports;
using DataStructures.Domain;
using DataStructures.Domain.Payments;

namespace DataStructures.Application.Payment;

public sealed class PaymentApplicationService(
  IFnbReadPort readPort,
  IOrderPort orderPort,
  IPaymentAggregatePort paymentAggregatePort,
  IPaymentGatewayPort paymentGatewayPort)
{
  public async Task<PaymentResult> ChargeOrderAsync(Order order, PaymentMethod method, Guid paymentAttemptId, CancellationToken cancellationToken = default)
  {
    var menu = await readPort.GetMenuAsync(cancellationToken);
    var total = CalculateTotal(order, menu);

    var payment = await paymentAggregatePort.FindByOrderIdAsync(order.Id, cancellationToken)
      ?? Domain.Payments.Payment.CreateNew(order.Id, total, method);

    if (payment.IsCaptured())
    {
      return new PaymentResult(order.Id, payment.Amount, payment.Method, payment.Reference!, payment.Status, payment.RetryCount, payment.FailureReason);
    }

    // Application/background layer decides when to trigger retry; aggregate decides if allowed.
    _ = payment.TryStartRetryAttempt();

    try
    {
      var paymentReference = await paymentGatewayPort.ChargeAsync(order.Id, total, method, paymentAttemptId, cancellationToken);
      payment.Authorize(paymentReference);
      payment.Capture();

      order.MarkPaid();
      await orderPort.SaveAsync(order, cancellationToken);
    }
    catch (Exception ex)
    {
      payment.Fail(ex.Message);
      await paymentAggregatePort.SaveAsync(payment, cancellationToken);
      throw;
    }

    await paymentAggregatePort.SaveAsync(payment, cancellationToken);

    return new PaymentResult(order.Id, payment.Amount, payment.Method, payment.Reference!, payment.Status, payment.RetryCount, payment.FailureReason);
  }

  private static decimal CalculateTotal(Order order, IReadOnlyDictionary<string, MenuItem> menu)
  {
    return order.Items.Sum(item => menu[item.Key].Price * item.Value);
  }
}
