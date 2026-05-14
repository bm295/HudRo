using DataStructures.Application.Models;
using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Application.UseCases;

public sealed class OrderApplicationService(IFnbReadPort readPort, IOrderPort orderPort)
{
  public async Task<Guid> CreateOrderAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
  {
    var table = (await readPort.GetTablesAsync(cancellationToken))
      .SingleOrDefault(t => string.Equals(t.Id, command.TableId, StringComparison.Ordinal));

    if (table is null)
    {
      throw new KeyNotFoundException($"Unknown table: {command.TableId}");
    }

    if (command.Guests > table.Seats)
    {
      throw new InvalidOperationException($"Table {table.Id} has {table.Seats} seats but received {command.Guests} guests.");
    }

    var current = await orderPort.FindOpenOrderByTableAsync(command.TableId, cancellationToken);
    if (current is not null)
    {
      throw new InvalidOperationException($"Table {command.TableId} already has an active order.");
    }

    var order = new Order(Guid.NewGuid(), command.TableId, command.Guests, DateTimeOffset.UtcNow);
    await orderPort.SaveAsync(order, cancellationToken);
    return order.Id;
  }

  public async Task AddItemAsync(AddItemCommand command, CancellationToken cancellationToken = default)
  {
    var order = await LoadOrderAsync(command.OrderId, cancellationToken);
    var menu = await readPort.GetMenuAsync(cancellationToken);

    if (!menu.ContainsKey(command.MenuCode))
    {
      throw new KeyNotFoundException($"Unknown menu code: {command.MenuCode}");
    }

    order.AddItem(OrderItem.Create(command.MenuCode, command.Quantity));
    await orderPort.SaveAsync(order, cancellationToken);
  }

  public async Task RemoveItemAsync(RemoveItemCommand command, CancellationToken cancellationToken = default)
  {
    var order = await LoadOrderAsync(command.OrderId, cancellationToken);
    order.RemoveItem(command.MenuCode, command.Quantity);
    await orderPort.SaveAsync(order, cancellationToken);
  }

  public async Task SendToKitchenAsync(Guid orderId, CancellationToken cancellationToken = default)
  {
    var order = await LoadOrderAsync(orderId, cancellationToken);
    order.SendToKitchen();
    await orderPort.SaveAsync(order, cancellationToken);
  }

  public async Task<CloseOrderResult> CloseOrderStateOnlyAsync(Guid orderId, PaymentResult paymentResult, CancellationToken cancellationToken = default)
  {
    var order = await LoadOrderAsync(orderId, cancellationToken);
    var menu = await readPort.GetMenuAsync(cancellationToken);

    var lines = order.Items
      .Select(x =>
      {
        var menuItem = menu[x.Key];
        var lineTotal = menuItem.Price * x.Value;
        return new BillLine(menuItem.Name, x.Value, menuItem.Price, lineTotal);
      })
      .ToArray();

    var total = lines.Sum(l => l.LineTotal);

    order.Close();
    await orderPort.SaveAsync(order, cancellationToken);

    var bill = new TableBill(
      order.Id,
      order.TableId,
      order.Guests,
      lines,
      total,
      paymentResult.Method,
      paymentResult.PaymentReference);

    return new CloseOrderResult(bill);
  }

  public async Task<Order> LoadOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
  {
    return await orderPort.FindByIdAsync(orderId, cancellationToken)
        ?? throw new KeyNotFoundException($"Order not found: {orderId}");
  }
}
