using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Infrastructure;

public sealed class FakePaymentGatewayAdapter : IPaymentGatewayPort
{
  public Task<string> ChargeAsync(Guid orderId, decimal amount, PaymentMethod paymentMethod, Guid paymentAttemptId, CancellationToken cancellationToken)
  {
    if (amount <= 0)
    {
      throw new InvalidOperationException($"Cannot charge non-positive amount for order {orderId}.");
    }

    var paymentRef = $"PAY-{paymentAttemptId:N}-{orderId.ToString()[..8]}-{paymentMethod}";
    return Task.FromResult(paymentRef);
  }
}
