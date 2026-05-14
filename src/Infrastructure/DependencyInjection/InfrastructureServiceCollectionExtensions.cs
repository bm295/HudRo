using DataStructures.Application.Ports;
using DataStructures.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace DataStructures.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
  public static IServiceCollection AddHudRoInMemoryStore(this IServiceCollection services)
  {
    services.AddSingleton(new InMemoryFnbStore
    {
      Profile = new RestaurantProfile("HudRo", 40, 60),
      Tables = new[]
      {
        new DiningTable("T01", 4),
        new DiningTable("T02", 4),
        new DiningTable("T03", 2),
        new DiningTable("T04", 6),
        new DiningTable("T05", 6),
        new DiningTable("T06", 6),
        new DiningTable("T07", 4),
        new DiningTable("T08", 4),
        new DiningTable("T09", 8),
        new DiningTable("T10", 8),
      }.ToDictionary(t => t.Id, StringComparer.Ordinal),
      Menu = new[]
      {
        new MenuItem("APP01", "Oyster Rockefeller", 260_000m),
        new MenuItem("MAIN01", "US Prime Ribeye", 950_000m),
        new MenuItem("MAIN02", "Atlantic Salmon", 520_000m),
        new MenuItem("DRINK01", "Signature Cocktail", 220_000m),
        new MenuItem("DRINK02", "Sparkling Water", 120_000m),
      }.ToDictionary(m => m.Code, StringComparer.Ordinal),
      Inventory = new Dictionary<string, InventoryItem>(StringComparer.Ordinal)
      {
        ["APP01"] = new("APP01", "Oyster Rockefeller", 50),
        ["MAIN01"] = new("MAIN01", "US Prime Ribeye", 50),
        ["MAIN02"] = new("MAIN02", "Atlantic Salmon", 50),
        ["DRINK01"] = new("DRINK01", "Signature Cocktail", 100),
        ["DRINK02"] = new("DRINK02", "Sparkling Water", 100),
      },
      Orders = new Dictionary<Guid, Order>(),
    });

    return services;
  }

  public static IServiceCollection AddInMemoryAdapters(this IServiceCollection services)
  {
    services.AddSingleton<IFnbReadPort, InMemoryFnbReadAdapter>();
    services.AddSingleton<IOrderPort, InMemoryOrderAdapter>();
    services.AddSingleton<IInventoryPort, InMemoryInventoryAdapter>();
    services.AddSingleton<IPaymentPort, FakePaymentGatewayAdapter>();

    return services;
  }
}
