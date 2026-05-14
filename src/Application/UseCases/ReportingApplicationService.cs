using DataStructures.Application.Models;
using DataStructures.Application.Ports;
using DataStructures.Domain;

namespace DataStructures.Application.UseCases;

public sealed class ReportingApplicationService(IFnbReadPort readPort)
{
  public async Task<ServiceSummaryResult> BuildDailySummaryAsync(BuildServiceSummaryQuery query, CancellationToken cancellationToken = default)
  {
    await ValidateCapacityAsync(cancellationToken);

    var profile = await readPort.GetProfileAsync(cancellationToken);
    var tables = await readPort.GetTablesAsync(cancellationToken);
    var closedOrders = await readPort.GetClosedOrdersAsync(cancellationToken);
    var menu = await readPort.GetMenuAsync(cancellationToken);

    var bills = closedOrders
      .Select(order =>
      {
        var lines = order.Items
          .Select(x =>
          {
            var menuItem = menu[x.Key];
            var lineTotal = menuItem.Price * x.Value;
            return new BillLine(menuItem.Name, x.Value, menuItem.Price, lineTotal);
          })
          .ToArray();

        return new TableBill(order.Id, order.TableId, order.Guests, lines, lines.Sum(l => l.LineTotal), PaymentMethod.Card, "N/A");
      })
      .ToArray();

    return new ServiceSummaryResult(
      profile,
      tables.Sum(t => t.Seats),
      bills.Sum(b => b.Guests),
      bills.Length,
      bills.Sum(b => b.Total),
      bills);
  }

  public async Task ValidateCapacityAsync(CancellationToken cancellationToken = default)
  {
    var profile = await readPort.GetProfileAsync(cancellationToken);
    var seats = (await readPort.GetTablesAsync(cancellationToken)).Sum(t => t.Seats);

    if (seats < profile.MinSeats || seats > profile.MaxSeats)
    {
      throw new InvalidOperationException(
        $"Configured seats ({seats}) are outside required range {profile.MinSeats}-{profile.MaxSeats}.");
    }
  }
}
