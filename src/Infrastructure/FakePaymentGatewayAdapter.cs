using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Infrastructure;

public sealed class FakePaymentGatewayAdapter : IPaymentPort
{
  public Task<string> ChargeAsync(Guid orderId, decimal amount, PaymentMethod paymentMethod, CancellationToken cancellationToken)
  {
    if (amount <= 0)
    {
      throw new InvalidOperationException($"Cannot charge non-positive amount for order {orderId}.");
    }

    var paymentRef = $"PAY-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{orderId.ToString()[..8]}-{paymentMethod}";
    return Task.FromResult(paymentRef);
  }
}
