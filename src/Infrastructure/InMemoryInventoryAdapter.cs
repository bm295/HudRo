using DataStructures.Application.Ports;

namespace DataStructures.Infrastructure;

public sealed class InMemoryInventoryAdapter(InMemoryFnbStore store) : IInventoryPort
{
  public Task EnsureAvailableAsync(string sku, int quantity, CancellationToken cancellationToken)
  {
    if (!store.Inventory.TryGetValue(sku, out var item))
    {
      throw new KeyNotFoundException($"Unknown inventory sku: {sku}");
    }

    if (quantity <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
    }

    if (item.QuantityOnHand < quantity)
    {
      throw new InvalidOperationException(
        $"Not enough stock for {sku}. Requested={quantity}, Available={item.QuantityOnHand}");
    }

    return Task.CompletedTask;
  }

  public Task DeductAsync(string sku, int quantity, CancellationToken cancellationToken)
  {
    if (!store.Inventory.TryGetValue(sku, out var item))
    {
      throw new KeyNotFoundException($"Unknown inventory sku: {sku}");
    }

    store.Inventory[sku] = item with { QuantityOnHand = item.QuantityOnHand - quantity };
    return Task.CompletedTask;
  }

  public Task RestoreAsync(string sku, int quantity, CancellationToken cancellationToken)
  {
    if (!store.Inventory.TryGetValue(sku, out var item))
    {
      throw new KeyNotFoundException($"Unknown inventory sku: {sku}");
    }

    if (quantity <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
    }

    store.Inventory[sku] = item with { QuantityOnHand = item.QuantityOnHand + quantity };
    return Task.CompletedTask;
  }
}
