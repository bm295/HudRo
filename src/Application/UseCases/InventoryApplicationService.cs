using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Application.UseCases;

public sealed class InventoryApplicationService(IInventoryPort inventoryPort)
{
  public async Task EnsureAvailableAsync(Order order, CancellationToken cancellationToken = default)
  {
    foreach (var (sku, qty) in order.Items)
    {
      await inventoryPort.EnsureAvailableAsync(sku, qty, cancellationToken);
    }
  }

  public async Task DeductAsync(Order order, CancellationToken cancellationToken = default)
  {
    foreach (var (sku, qty) in order.Items)
    {
      await inventoryPort.DeductAsync(sku, qty, cancellationToken);
    }
  }
}
