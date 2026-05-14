using DataStructures.Domain;

namespace DataStructures.Application.Ports;

public interface IOrderPort
{
  Task<Order?> FindByIdAsync(Guid orderId, CancellationToken cancellationToken);
  Task<Order?> FindOpenOrderByTableAsync(string tableId, CancellationToken cancellationToken);
  Task SaveAsync(Order order, CancellationToken cancellationToken);
}
