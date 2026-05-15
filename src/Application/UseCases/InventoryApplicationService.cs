using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Application.Inventory;

public sealed class InventoryApplicationService(IInventoryPort inventoryPort)
{
  public async Task ReserveAsync(Order order, CancellationToken cancellationToken = default)
  {
    foreach (var (sku, qty) in order.Items)
    {
      var item = await RequireItemAsync(sku, cancellationToken);
      item.Reserve(qty);
      await inventoryPort.SaveAsync(item, cancellationToken);
    }
  }

  public async Task DeductReservedAsync(Order order, CancellationToken cancellationToken = default)
  {
    foreach (var (sku, qty) in order.Items)
    {
      var item = await RequireItemAsync(sku, cancellationToken);
      item.TryDeductReservedForRetry(qty);
      await inventoryPort.SaveAsync(item, cancellationToken);
    }
  }

  public async Task ReleaseAsync(Order order, CancellationToken cancellationToken = default)
  {
    foreach (var (sku, qty) in order.Items)
    {
      var item = await RequireItemAsync(sku, cancellationToken);
      item.Release(qty);
      await inventoryPort.SaveAsync(item, cancellationToken);
    }
  }

  private async Task<InventoryItem> RequireItemAsync(string sku, CancellationToken cancellationToken)
  {
    var item = await inventoryPort.FindBySkuAsync(sku, cancellationToken);
    return item ?? throw new KeyNotFoundException($"Unknown inventory sku: {sku}");
  }
}
