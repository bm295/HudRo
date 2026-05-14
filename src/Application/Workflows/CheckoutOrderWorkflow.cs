using DataStructures.Application.Models;
using DataStructures.Application.Inventory;
using DataStructures.Application.Payment;
using DataStructures.Domain;

namespace DataStructures.Application.Workflows;

public sealed class CheckoutOrderWorkflow(
  OrderApplicationService orderService,
  InventoryApplicationService inventoryService,
  PaymentApplicationService paymentService)
{
  private static readonly Dictionary<Guid, CheckoutProgress> ProgressBySession = new();

  public async Task<CloseOrderResult> ExecuteAsync(CheckoutOrderCommand command, CancellationToken cancellationToken = default)
  {
    var progress = GetOrCreateProgress(command.CheckoutSessionId, command.OrderId, command.PaymentAttemptId);
    var order = await orderService.LoadOrderAsync(command.OrderId, cancellationToken);
    ValidateReadyToCheckout(order);

    try
    {
      if (!progress.InventoryDeducted)
      {
        await inventoryService.EnsureAvailableAsync(order, cancellationToken);
        await inventoryService.DeductAsync(order, cancellationToken);
        progress.InventoryDeducted = true;
      }

      if (progress.PaymentResult is null)
      {
        progress.PaymentResult = await paymentService.ChargeOrderAsync(order, command.Method, command.PaymentAttemptId, cancellationToken);
      }

      if (!progress.OrderClosed)
      {
        progress.CloseOrderResult = await orderService.CloseOrderStateOnlyAsync(order.Id, progress.PaymentResult, cancellationToken);
        progress.OrderClosed = true;
      }

      return progress.CloseOrderResult!;
    }
    catch
    {
      if (progress.InventoryDeducted && progress.PaymentResult is null)
      {
        await inventoryService.RestoreAsync(order, cancellationToken);
        progress.InventoryDeducted = false;
      }

      throw;
    }
  }

  private static CheckoutProgress GetOrCreateProgress(Guid checkoutSessionId, Guid orderId, Guid paymentAttemptId)
  {
    if (ProgressBySession.TryGetValue(checkoutSessionId, out var existing))
    {
      if (existing.OrderId != orderId)
      {
        throw new InvalidOperationException($"CheckoutSessionId {checkoutSessionId} already belongs to order {existing.OrderId}.");
      }

      if (existing.PaymentAttemptId != paymentAttemptId)
      {
        throw new InvalidOperationException($"CheckoutSessionId {checkoutSessionId} already uses PaymentAttemptId {existing.PaymentAttemptId}.");
      }

      return existing;
    }

    var progress = new CheckoutProgress(orderId, paymentAttemptId);
    ProgressBySession[checkoutSessionId] = progress;
    return progress;
  }

  private static void ValidateReadyToCheckout(Order order)
  {
    if (order.Status != OrderStatus.SentToKitchen)
    {
      throw new InvalidOperationException($"Order {order.Id} is not ready for checkout. Current status: {order.Status}.");
    }

    if (order.Items.Count == 0)
    {
      throw new InvalidOperationException($"Order {order.Id} has no items and cannot be checked out.");
    }
  }

  private sealed record CheckoutProgress(Guid OrderId, Guid PaymentAttemptId)
  {
    public bool InventoryDeducted { get; set; }
    public bool OrderClosed { get; set; }
    public PaymentResult? PaymentResult { get; set; }
    public CloseOrderResult? CloseOrderResult { get; set; }
  }
}
