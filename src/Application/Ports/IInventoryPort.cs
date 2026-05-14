namespace DataStructures.Application.Ports;

public interface IInventoryPort
{
  Task EnsureAvailableAsync(string sku, int quantity, CancellationToken cancellationToken);
  Task DeductAsync(string sku, int quantity, CancellationToken cancellationToken);
  Task RestoreAsync(string sku, int quantity, CancellationToken cancellationToken);
}
