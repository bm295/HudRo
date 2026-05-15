using DataStructures.Domain;
using DataStructures.Domain.Loyalty;
using DataStructures.Domain.Payments;
using Xunit;

namespace HudRo.UnitTests;

public sealed class DomainStateMachineTests
{
  [Fact]
  public void Payment_ShouldRetryUntilMax_ThenReject()
  {
    var payment = Payment.CreateNew(Guid.NewGuid(), 100_000m, PaymentMethod.Card);

    payment.Fail("timeout");
    Assert.True(payment.CanRetry());
    payment.Retry();

    payment.Fail("timeout-2");
    payment.Retry();

    payment.Fail("timeout-3");
    payment.Retry();

    payment.Fail("timeout-4");
    Assert.False(payment.CanRetry());
    Assert.Throws<InvalidOperationException>(() => payment.Retry());
  }

  [Fact]
  public void Inventory_ShouldReserveDeductAndRelease()
  {
    var item = new InventoryItem("SKU-1", "Milk", 10);

    item.Reserve(4);
    Assert.Equal(4, item.QuantityReserved);
    Assert.Equal(10, item.QuantityOnHand);

    item.DeductReserved(3);
    Assert.Equal(1, item.QuantityReserved);
    Assert.Equal(7, item.QuantityOnHand);

    item.Release(1);
    Assert.Equal(0, item.QuantityReserved);
    Assert.Equal(7, item.QuantityOnHand);
  }

  [Fact]
  public void Inventory_ShouldProtectStockInvariants()
  {
    var item = new InventoryItem("SKU-1", "Milk", 5);

    Assert.Throws<InvalidOperationException>(() => item.Adjust(-6));

    item.Reserve(3);
    Assert.Throws<InvalidOperationException>(() => item.Adjust(-3));
  }

  [Fact]
  public void Inventory_DeductRetry_ShouldBeSafeWhenAlreadyDeducted()
  {
    var item = new InventoryItem("SKU-1", "Milk", 10);
    item.Reserve(2);

    var first = item.TryDeductReservedForRetry(2);
    var second = item.TryDeductReservedForRetry(2);

    Assert.True(first);
    Assert.False(second);
    Assert.Equal(8, item.QuantityOnHand);
    Assert.Equal(0, item.QuantityReserved);
  }

  [Fact]
  public void Order_ShouldFollowKitchenLifecycle()
  {
    var order = new Order(Guid.NewGuid(), "T1", 2, DateTimeOffset.UtcNow);
    order.AddItem(OrderItem.Create("M1", 1));

    order.SendToKitchen();
    order.MarkPreparing();
    order.MarkServed();
    order.MarkPaid();
    order.Close();

    Assert.Equal(OrderStatus.Closed, order.Status);
  }

  [Fact]
  public void Order_ShouldEnforceKitchenTransitionGuards()
  {
    var order = new Order(Guid.NewGuid(), "T1", 2, DateTimeOffset.UtcNow);
    order.AddItem(OrderItem.Create("M1", 1));

    var preparingBeforeSend = Assert.Throws<InvalidOperationException>(() => order.MarkPreparing());
    Assert.Contains("SentToKitchen only from Draft", preparingBeforeSend.Message);

    order.SendToKitchen();
    order.MarkPreparing();
    order.MarkServed();

    var closeBeforePaid = Assert.Throws<InvalidOperationException>(() => order.Close());
    Assert.Contains("Closed only from Paid", closeBeforePaid.Message);
  }

  [Fact]
  public void Order_ShouldSupportExceptionalFlowsWithInvariants()
  {
    var draft = new Order(Guid.NewGuid(), "T1", 2, DateTimeOffset.UtcNow);
    draft.AddItem(OrderItem.Create("M1", 1));
    draft.CancelDraft();
    Assert.Equal(OrderStatus.Closed, draft.Status);

    var paidOrder = new Order(Guid.NewGuid(), "T2", 2, DateTimeOffset.UtcNow);
    paidOrder.AddItem(OrderItem.Create("M1", 1));
    paidOrder.SendToKitchen();
    paidOrder.MarkPreparing();
    paidOrder.MarkServed();
    paidOrder.MarkPaid();
    paidOrder.VoidAfterPayment("customer chargeback");
    Assert.Equal(OrderStatus.Closed, paidOrder.Status);

    paidOrder.Reopen();
    Assert.Equal(OrderStatus.Served, paidOrder.Status);
  }

  [Fact]
  public void Loyalty_ShouldAccrueRedeemAndReverse()
  {
    var account = new LoyaltyAccount(Guid.NewGuid(), Guid.NewGuid(), 0, LoyaltyTier.Bronze);
    var accrueTx = Guid.NewGuid();
    var redeemTx = Guid.NewGuid();

    account.Accrue(accrueTx, Guid.NewGuid(), 25_000m, DateTimeOffset.UtcNow);
    Assert.Equal(2, account.PointsBalance);

    account.Redeem(redeemTx, 1, DateTimeOffset.UtcNow);
    Assert.Equal(1, account.PointsBalance);

    account.Reverse(Guid.NewGuid(), redeemTx, DateTimeOffset.UtcNow);
    Assert.Equal(2, account.PointsBalance);
  }
}
