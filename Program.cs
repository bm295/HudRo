using DataStructures.Adapters.Console;
using DataStructures.Application.DependencyInjection;
using DataStructures.Application.Models;
using DataStructures.Application.Order;
using DataStructures.Application.Inventory;
using DataStructures.Application.Payment;
using DataStructures.Application.Reporting;
using DataStructures.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services
  .AddHudRoInMemoryStore()
  .AddInMemoryAdapters();

// Order module registrations
services.AddOrderModule();

// Inventory module registrations
services.AddInventoryModule();

// Payment module registrations
services.AddPaymentModule();

// Reporting module registrations
services.AddReportingModule();

services.AddSingleton<FnbConsolePresenter>();

using var provider = services.BuildServiceProvider();

var orderService = provider.GetRequiredService<OrderApplicationService>();
var checkoutWorkflow = provider.GetRequiredService<CheckoutOrderWorkflow>();
var reportingService = provider.GetRequiredService<ReportingApplicationService>();
var presenter = provider.GetRequiredService<FnbConsolePresenter>();

var orderId = await orderService.CreateOrderAsync(new CreateOrderCommand("T01", 4));
await orderService.AddItemAsync(new AddOrderItemCommand(orderId, "DRINK01", 4));
await orderService.AddItemAsync(new AddOrderItemCommand(orderId, "MAIN01", 4));
await orderService.RemoveItemAsync(new RemoveOrderItemCommand(orderId, "DRINK01", 1));
await orderService.SendToKitchenAsync(orderId);

await checkoutWorkflow.ExecuteAsync(new CheckoutOrderCommand(orderId, PaymentMethod.Card, Guid.NewGuid(), Guid.NewGuid()));

var summary = await reportingService.BuildDailySummaryAsync(new BuildServiceSummaryQuery(DateOnly.FromDateTime(DateTime.UtcNow)));
presenter.Show(summary);
