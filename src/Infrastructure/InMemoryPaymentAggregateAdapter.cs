using DataStructures.Application.Ports;
using DataStructures.Domain.Payments;

namespace DataStructures.Infrastructure;

public sealed class InMemoryPaymentAggregateAdapter(InMemoryFnbStore store) : IPaymentAggregatePort
{
  public Task<Payment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
  {
    var payment = store.Payments.Values.SingleOrDefault(x => x.OrderId == orderId);
    return Task.FromResult(payment);
  }

  public Task SaveAsync(Payment payment, CancellationToken cancellationToken)
  {
    store.Payments[payment.Id] = payment;
    return Task.CompletedTask;
  }
}
