using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Infrastructure;

public sealed class InMemoryFnbReadAdapter(InMemoryFnbStore store) : IFnbReadPort
{
  public Task<RestaurantProfile> GetProfileAsync(CancellationToken cancellationToken)
    => Task.FromResult(store.Profile);

  public Task<IReadOnlyList<DiningTable>> GetTablesAsync(CancellationToken cancellationToken)
    => Task.FromResult<IReadOnlyList<DiningTable>>(store.Tables.Values.OrderBy(t => t.Id).ToArray());

  public Task<IReadOnlyDictionary<string, MenuItem>> GetMenuAsync(CancellationToken cancellationToken)
    => Task.FromResult<IReadOnlyDictionary<string, MenuItem>>(store.Menu);

  public Task<IReadOnlyList<Order>> GetClosedOrdersAsync(CancellationToken cancellationToken)
    => Task.FromResult<IReadOnlyList<Order>>(store.Orders.Values.Where(o => o.Status == OrderStatus.Closed).ToArray());
}
