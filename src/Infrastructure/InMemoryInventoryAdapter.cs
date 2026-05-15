using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Infrastructure;


public sealed class InMemoryInventoryAdapter(InMemoryFnbStore store) : IInventoryPort
{
  public Task ReserveAsync(string sku, int quantity, CancellationToken cancellationToken)
  {
    var item = RequireInventoryItem(sku);
    item.Reserve(quantity);
    return Task.CompletedTask;
  }

  public Task ReleaseAsync(string sku, int quantity, CancellationToken cancellationToken)
  {
    var item = RequireInventoryItem(sku);
    item.Release(quantity);
    return Task.CompletedTask;
  }

  public Task DeductReservedAsync(string sku, int quantity, CancellationToken cancellationToken)
  {
    var item = RequireInventoryItem(sku);
    item.DeductReserved(quantity);
    return Task.CompletedTask;
  }

  private InventoryItem RequireInventoryItem(string sku)
  {
    if (!store.Inventory.TryGetValue(sku, out var item))
    {
      throw new KeyNotFoundException($"Unknown inventory sku: {sku}");
    }

    return item;
  }
}
