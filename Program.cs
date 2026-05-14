using DataStructures.Adapters.Console;
using DataStructures.Application.Models;
using DataStructures.Application.Ports;
using DataStructures.Application.UseCases;
using DataStructures.Domain;
using DataStructures.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

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

services.AddSingleton<IFnbReadPort, InMemoryFnbReadAdapter>();
services.AddSingleton<IOrderPort, InMemoryOrderAdapter>();
services.AddSingleton<IInventoryPort, InMemoryInventoryAdapter>();
services.AddSingleton<IPaymentPort, FakePaymentGatewayAdapter>();
services.AddSingleton<OrderApplicationService>();
services.AddSingleton<InventoryApplicationService>();
services.AddSingleton<PaymentApplicationService>();
services.AddSingleton<CheckoutOrderWorkflow>();
services.AddSingleton<ReportingApplicationService>();
services.AddSingleton<RunHudRoFnbUseCase>();
services.AddSingleton<FnbConsolePresenter>();

using var provider = services.BuildServiceProvider();

var useCase = provider.GetRequiredService<RunHudRoFnbUseCase>();
var presenter = provider.GetRequiredService<FnbConsolePresenter>();

var orderId = await useCase.CreateOrderAsync(new CreateOrderCommand("T01", 4));
await useCase.AddItemAsync(new AddOrderItemCommand(orderId, "DRINK01", 4));
await useCase.AddItemAsync(new AddOrderItemCommand(orderId, "MAIN01", 4));
await useCase.RemoveItemAsync(new RemoveOrderItemCommand(orderId, "DRINK01", 1));
await useCase.SendToKitchenAsync(orderId);

await useCase.CheckoutOrderAsync(new ProcessPaymentCommand(orderId, PaymentMethod.Card));

var summary = await useCase.BuildDailySummaryAsync(new BuildServiceSummaryQuery(DateOnly.FromDateTime(DateTime.UtcNow)));
presenter.Show(summary);
