using System.Collections.Concurrent;
using DataStructures.Application.Models;
using DataStructures.Application.Inventory;
using DataStructures.Application.Ports;
using DataStructures.Application.Payment;
using DataStructures.Domain;
using DataStructures.Domain.Inventory;

namespace DataStructures.Application.Workflows;

public sealed class CheckoutOrderWorkflow(
  OrderApplicationService orderService,
  InventoryApplicationService inventoryService,
  PaymentApplicationService paymentService,
  ILoyaltyOperations loyaltyService)
{
  private static readonly ConcurrentDictionary<Guid, CheckoutProgress> ProgressBySession = new();

  public async Task<CloseOrderResult> ExecuteAsync(CheckoutOrderCommand command, CancellationToken cancellationToken = default)
  {
    var progress = GetOrCreateProgress(command.CheckoutSessionId, command.OrderId, command.PaymentAttemptId);
    var order = await orderService.LoadOrderAsync(command.OrderId, cancellationToken);

    order.EnsureReadyForCheckout();

    try
    {
      await ReserveInventoryAsync(order, progress, cancellationToken);
      await AuthorizeAndCapturePaymentAsync(order, command, progress, cancellationToken);
      await DeductReservedInventoryAsync(order, progress, cancellationToken);
      await CloseOrderAsync(order, progress, cancellationToken);
      await TriggerLoyaltyIntentAsync(command, progress, cancellationToken);

      return progress.CloseOrderResult!;
    }
    catch
    {
      await CompensateAsync(order, progress, cancellationToken);
      throw;
    }
  }

  private static CheckoutProgress GetOrCreateProgress(Guid checkoutSessionId, Guid orderId, Guid paymentAttemptId)
  {
    return ProgressBySession.GetOrAdd(checkoutSessionId, _ => new CheckoutProgress(orderId, paymentAttemptId));
  }

  private async Task ReserveInventoryAsync(Order order, CheckoutProgress progress, CancellationToken cancellationToken)
  {
    EnsureCommandConsistency(progress, order.Id, progress.PaymentAttemptId);

    if (!progress.InventoryLifecycle.IsReservationRequired())
    {
      return;
    }

    await inventoryService.ReserveAsync(order, cancellationToken);
    progress.InventoryLifecycle.MarkReserved();
  }

  private async Task DeductReservedInventoryAsync(Order order, CheckoutProgress progress, CancellationToken cancellationToken)
  {
    if (!progress.InventoryLifecycle.IsDeductionRequired())
    {
      return;
    }

    await inventoryService.DeductReservedAsync(order, cancellationToken);
    progress.InventoryLifecycle.MarkDeducted();
  }

  private async Task AuthorizeAndCapturePaymentAsync(
    Order order,
    CheckoutOrderCommand command,
    CheckoutProgress progress,
    CancellationToken cancellationToken)
  {
    EnsureCommandConsistency(progress, command.OrderId, command.PaymentAttemptId);

    if (progress.PaymentResult is not null)
    {
      return;
    }

    progress.PaymentResult = await paymentService.ChargeOrderAsync(order, command.Method, command.PaymentAttemptId, cancellationToken);
  }

  private async Task CloseOrderAsync(Order order, CheckoutProgress progress, CancellationToken cancellationToken)
  {
    if (progress.CloseOrderResult is not null)
    {
      return;
    }

    if (progress.PaymentResult is null)
    {
      throw new InvalidOperationException("Cannot close order before payment is captured.");
    }

    progress.CloseOrderResult = await orderService.CloseOrderStateOnlyAsync(order.Id, progress.PaymentResult, cancellationToken);
  }

  private async Task CompensateAsync(Order order, CheckoutProgress progress, CancellationToken cancellationToken)
  {
    // Compensation policy:
    // - Failed before payment capture: restore deducted inventory.
    // - Failed after payment capture: do NOT auto-refund here to avoid accidental double-refund;
    //   keep session idempotency and allow safe retry to finish close-order step.
    if (progress.InventoryLifecycle.ShouldReleaseOnFailureWithoutPayment() && progress.PaymentResult is null)
    {
      await inventoryService.ReleaseAsync(order, cancellationToken);
      progress.InventoryLifecycle.MarkReleased();
    }
  }

  private static void EnsureCommandConsistency(CheckoutProgress progress, Guid orderId, Guid paymentAttemptId)
  {
    if (progress.OrderId != orderId)
    {
      throw new InvalidOperationException($"Checkout session is bound to order {progress.OrderId}, but received {orderId}.");
    }

    if (progress.PaymentAttemptId != paymentAttemptId)
    {
      throw new InvalidOperationException($"Checkout session is bound to payment attempt {progress.PaymentAttemptId}, but received {paymentAttemptId}.");
    }
  }


  private async Task TriggerLoyaltyIntentAsync(CheckoutOrderCommand command, CheckoutProgress progress, CancellationToken cancellationToken)
  {
    if (progress.LoyaltyIntentTriggered || progress.CloseOrderResult is null)
    {
      return;
    }

    await loyaltyService.AccrueAsync(
      new LoyaltyAccrualIntent(
        command.OrderId,
        command.CustomerId,
        progress.CloseOrderResult.Bill.Total,
        Guid.NewGuid(),
        DateTimeOffset.UtcNow),
      cancellationToken);

    progress.LoyaltyIntentTriggered = true;
  }

  private sealed record CheckoutProgress(Guid OrderId, Guid PaymentAttemptId)
  {
    public InventoryCheckoutLifecycle InventoryLifecycle { get; } = new();
    public PaymentResult? PaymentResult { get; set; }
    public CloseOrderResult? CloseOrderResult { get; set; }
    public bool LoyaltyIntentTriggered { get; set; }
  }
}
