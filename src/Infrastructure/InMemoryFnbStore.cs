using DataStructures.Domain;

namespace DataStructures.Infrastructure;

public sealed class InMemoryFnbStore
{
  public required RestaurantProfile Profile { get; init; }
  public required Dictionary<string, DiningTable> Tables { get; init; }
  public required Dictionary<string, MenuItem> Menu { get; init; }
  public required Dictionary<string, InventoryItem> Inventory { get; init; }
  public required Dictionary<Guid, Order> Orders { get; init; }
}
