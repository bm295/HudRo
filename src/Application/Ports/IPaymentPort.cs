using DataStructures.Domain;

namespace DataStructures.Application.Ports;

public interface IPaymentPort
{
  Task<string> ChargeAsync(Guid orderId, decimal amount, PaymentMethod paymentMethod, CancellationToken cancellationToken);
}
