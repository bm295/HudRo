using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Application.Inventory;

public sealed class InventoryApplicationService(IInventoryPort inventoryPort)
{
  public async Task ReserveAsync(Order order, CancellationToken cancellationToken = default)
  {
    foreach (var (sku, qty) in order.Items)
    {
      await inventoryPort.ReserveAsync(sku, qty, cancellationToken);
    }
  }

  public async Task DeductReservedAsync(Order order, CancellationToken cancellationToken = default)
  {
    foreach (var (sku, qty) in order.Items)
    {
      await inventoryPort.DeductReservedAsync(sku, qty, cancellationToken);
    }
  }

  public async Task ReleaseAsync(Order order, CancellationToken cancellationToken = default)
  {
    foreach (var (sku, qty) in order.Items)
    {
      await inventoryPort.ReleaseAsync(sku, qty, cancellationToken);
    }
  }
}
