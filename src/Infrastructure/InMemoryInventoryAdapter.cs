using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Infrastructure;


public sealed class InMemoryInventoryAdapter(InMemoryFnbStore store) : IInventoryPort
{
  public Task<InventoryItem?> FindBySkuAsync(string sku, CancellationToken cancellationToken)
  {
    store.Inventory.TryGetValue(sku, out var item);
    return Task.FromResult(item is null ? null : new InventoryItem(item.Sku, item.Name, item.QuantityOnHand, item.QuantityReserved));
  }

  public Task SaveAsync(InventoryItem item, CancellationToken cancellationToken)
  {
    store.Inventory[item.Sku] = item;
    return Task.CompletedTask;
  }
}
