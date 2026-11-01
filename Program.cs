using DataStructures.Application.Assistant;
using DataStructures.Infrastructure.Assistant;
using System.Text.Json;
using DataStructures.Application.DependencyInjection;
using DataStructures.Application.Models;
using DataStructures.Application.Order;
using DataStructures.Application.Reporting;
using DataStructures.Application.Workflows;
using DataStructures.Domain;
using DataStructures.Infrastructure;
using DataStructures.Infrastructure.DependencyInjection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
  options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services
  .AddHudRoInMemoryStore()
  .AddInMemoryAdapters();

builder.Services.AddOrderModule();
builder.Services.AddInventoryModule();
builder.Services.AddPaymentModule();
builder.Services.AddLoyaltyModule();
builder.Services.AddReportingModule();

var assistantMode = builder.Configuration["Assistant:Mode"] ?? "Demo";
if (assistantMode.Equals("Demo", StringComparison.OrdinalIgnoreCase))
  builder.Services.AddSingleton<IAssistantModel, DemoAssistantModel>();
else if (assistantMode.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
  builder.Services.AddHttpClient<IAssistantModel, OpenAiAssistantModel>(client => client.Timeout = TimeSpan.FromSeconds(30));
else
  throw new InvalidOperationException("Assistant:Mode must be Demo or OpenAI.");
builder.Services.AddScoped<MenuAssistant>();

var app = builder.Build();

app.MapPost("/api/assistant/menu", async (AssistantRequest request, MenuAssistant assistant, CancellationToken ct) =>
{
  try { return Results.Ok(await assistant.RunAsync(request, ct)); }
  catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
  catch (Exception ex) when (ex is InvalidDataException or JsonException or HttpRequestException or KeyNotFoundException)
  { return Results.Problem("Assistant returned an invalid response or provider is unavailable.", statusCode: 502); }
  catch (OperationCanceledException) when (!ct.IsCancellationRequested)
  { return Results.Problem("Assistant provider timed out.", statusCode: 504); }
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/operations", async (
  InMemoryFnbStore store,
  ReportingApplicationService reportingService,
  CancellationToken cancellationToken) =>
{
  var summary = await reportingService.BuildDailySummaryAsync(
    new BuildServiceSummaryQuery(DateOnly.FromDateTime(DateTime.UtcNow)),
    cancellationToken);

  var openOrders = store.Orders.Values
    .Where(order => order.Status is not OrderStatus.Closed)
    .OrderBy(order => order.OpenedAtUtc)
    .Select(order => ToOrderView(order, store.Menu))
    .ToArray();

  var activeByTable = openOrders.ToDictionary(order => order.TableId, StringComparer.Ordinal);

  return Results.Ok(new OperationsView(
    store.Profile,
    store.Tables.Values.OrderBy(table => table.Id).Select(table =>
    {
      activeByTable.TryGetValue(table.Id, out var order);
      return new TableView(table.Id, table.Seats, order?.Id, order?.Status);
    }).ToArray(),
    store.Menu.Values.OrderBy(item => item.Code).ToArray(),
    store.Inventory.Values.OrderBy(item => item.Sku).ToArray(),
    openOrders,
    summary));
});

app.MapPost("/api/orders", async (
  CreateOrderRequest request,
  OrderApplicationService orderService,
  CancellationToken cancellationToken) =>
{
  try
  {
    var orderId = await orderService.CreateOrderAsync(
      new CreateOrderCommand(request.TableId, request.Guests),
      cancellationToken);

    return Results.Created($"/api/orders/{orderId}", new { orderId });
  }
  catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or ArgumentException)
  {
    return BusinessError(ex);
  }
});

app.MapPost("/api/orders/{orderId:guid}/items", async (
  Guid orderId,
  ChangeItemRequest request,
  OrderApplicationService orderService,
  CancellationToken cancellationToken) =>
{
  try
  {
    await orderService.AddItemAsync(new AddOrderItemCommand(orderId, request.MenuCode, request.Quantity), cancellationToken);
    return Results.NoContent();
  }
  catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or ArgumentException)
  {
    return BusinessError(ex);
  }
});

app.MapPost("/api/orders/{orderId:guid}/items/remove", async (
  Guid orderId,
  ChangeItemRequest request,
  OrderApplicationService orderService,
  CancellationToken cancellationToken) =>
{
  try
  {
    await orderService.RemoveItemAsync(new RemoveOrderItemCommand(orderId, request.MenuCode, request.Quantity), cancellationToken);
    return Results.NoContent();
  }
  catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or ArgumentException)
  {
    return BusinessError(ex);
  }
});

app.MapPost("/api/orders/{orderId:guid}/send", async (
  Guid orderId,
  OrderApplicationService orderService,
  CancellationToken cancellationToken) =>
{
  try
  {
    await orderService.SendToKitchenAsync(orderId, cancellationToken);
    return Results.NoContent();
  }
  catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
  {
    return BusinessError(ex);
  }
});

app.MapPost("/api/orders/{orderId:guid}/prepare", async (
  Guid orderId,
  OrderApplicationService orderService,
  CancellationToken cancellationToken) =>
{
  try
  {
    await orderService.MarkPreparingAsync(orderId, cancellationToken);
    return Results.NoContent();
  }
  catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
  {
    return BusinessError(ex);
  }
});

app.MapPost("/api/orders/{orderId:guid}/serve", async (
  Guid orderId,
  OrderApplicationService orderService,
  CancellationToken cancellationToken) =>
{
  try
  {
    await orderService.MarkServedAsync(orderId, cancellationToken);
    return Results.NoContent();
  }
  catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
  {
    return BusinessError(ex);
  }
});

app.MapPost("/api/orders/{orderId:guid}/checkout", async (
  Guid orderId,
  CheckoutRequest request,
  CheckoutOrderWorkflow checkoutWorkflow,
  CancellationToken cancellationToken) =>
{
  try
  {
    var result = await checkoutWorkflow.ExecuteAsync(
      new CheckoutOrderCommand(orderId, request.Method, Guid.NewGuid(), Guid.NewGuid(), request.CustomerId ?? Guid.NewGuid()),
      cancellationToken);

    return Results.Ok(result);
  }
  catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or ArgumentException)
  {
    return BusinessError(ex);
  }
});

app.MapFallbackToFile("index.html");

app.Run();

static IResult BusinessError(Exception ex)
  => Results.BadRequest(new { error = ex.Message });

static OrderView ToOrderView(Order order, IReadOnlyDictionary<string, MenuItem> menu)
{
  var lines = order.Items
    .Select(item =>
    {
      var menuItem = menu[item.Key];
      return new OrderLineView(item.Key, menuItem.Name, item.Value, menuItem.Price, item.Value * menuItem.Price);
    })
    .ToArray();

  return new OrderView(
    order.Id,
    order.TableId,
    order.Guests,
    order.Status.ToString(),
    lines,
    lines.Sum(line => line.LineTotal),
    order.OpenedAtUtc);
}

public sealed record CreateOrderRequest(string TableId, int Guests);
public sealed record ChangeItemRequest(string MenuCode, int Quantity);
public sealed record CheckoutRequest(PaymentMethod Method, Guid? CustomerId);
public sealed record TableView(string Id, int Seats, Guid? ActiveOrderId, string? Status);
public sealed record OrderLineView(string MenuCode, string Name, int Quantity, decimal UnitPrice, decimal LineTotal);
public sealed record OrderView(Guid Id, string TableId, int Guests, string Status, IReadOnlyList<OrderLineView> Lines, decimal Total, DateTimeOffset OpenedAtUtc);
public sealed record OperationsView(
  RestaurantProfile Profile,
  IReadOnlyList<TableView> Tables,
  IReadOnlyList<MenuItem> Menu,
  IReadOnlyList<InventoryItem> Inventory,
  IReadOnlyList<OrderView> OpenOrders,
  ServiceSummaryResult Summary);
