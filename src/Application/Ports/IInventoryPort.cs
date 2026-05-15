using DataStructures.Domain;

namespace DataStructures.Application.Ports;

public interface IInventoryPort
{
  Task<InventoryItem?> FindBySkuAsync(string sku, CancellationToken cancellationToken);
  Task SaveAsync(InventoryItem item, CancellationToken cancellationToken);
}
