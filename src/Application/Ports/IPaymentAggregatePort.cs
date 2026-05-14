using DataStructures.Domain.Payments;

namespace DataStructures.Application.Ports;

public interface IPaymentAggregatePort
{
  Task<Payment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
  Task SaveAsync(Payment payment, CancellationToken cancellationToken);
}
