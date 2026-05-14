using DataStructures.Application.Models;

namespace DataStructures.Adapters.Console;

public sealed class FnbConsolePresenter
{
  public void Show(ServiceSummaryResult summary)
  {
    Console.WriteLine("=== FnB Management Demo ===");
    Console.WriteLine($"Restaurant: {summary.Profile.Name}");
    Console.WriteLine($"Seat requirement: {summary.Profile.MinSeats}-{summary.Profile.MaxSeats}");
    Console.WriteLine($"Configured seats: {summary.ConfiguredSeats}");
    Console.WriteLine();

    foreach (var bill in summary.Bills)
    {
      Console.WriteLine($"Order {bill.OrderId} - Table {bill.TableId} ({bill.Guests} guests)");
      foreach (var line in bill.Lines)
      {
        Console.WriteLine($" - {line.ItemName} x{line.Quantity} @ {line.UnitPrice:N0} = {line.LineTotal:N0}");
      }

      Console.WriteLine($" Payment: {bill.PaymentMethod} / Ref: {bill.PaymentReference}");
      Console.WriteLine($" Total: {bill.Total:N0}");
      Console.WriteLine();
    }

    Console.WriteLine("Daily summary");
    Console.WriteLine($"Orders closed: {summary.OrdersClosed}");
    Console.WriteLine($"Guests served: {summary.ServedGuests}");
    Console.WriteLine($"Revenue: {summary.Revenue:N0}");
  }
}
