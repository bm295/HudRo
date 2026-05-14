using DataStructures.Domain;

namespace DataStructures.Application.Ports;

public interface IPaymentGatewayPort
{
  Task<string> ChargeAsync(Guid orderId, decimal amount, PaymentMethod paymentMethod, Guid paymentAttemptId, CancellationToken cancellationToken);
}
