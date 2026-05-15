namespace DataStructures.Application.Ports;

public interface IInventoryPort
{
  Task ReserveAsync(string sku, int quantity, CancellationToken cancellationToken);
  Task ReleaseAsync(string sku, int quantity, CancellationToken cancellationToken);
  Task DeductReservedAsync(string sku, int quantity, CancellationToken cancellationToken);
}
