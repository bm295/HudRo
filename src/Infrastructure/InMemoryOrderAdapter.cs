using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Infrastructure;

public sealed class InMemoryOrderAdapter(InMemoryFnbStore store) : IOrderPort
{
  public Task<Order?> FindByIdAsync(Guid orderId, CancellationToken cancellationToken)
  {
    store.Orders.TryGetValue(orderId, out var order);
    return Task.FromResult(order);
  }

  public Task<Order?> FindOpenOrderByTableAsync(string tableId, CancellationToken cancellationToken)
  {
    var order = store.Orders.Values
      .SingleOrDefault(x => x.TableId == tableId && x.Status is not OrderStatus.Closed);
    return Task.FromResult(order);
  }

  public Task SaveAsync(Order order, CancellationToken cancellationToken)
  {
    store.Orders[order.Id] = order;
    return Task.CompletedTask;
  }
}
