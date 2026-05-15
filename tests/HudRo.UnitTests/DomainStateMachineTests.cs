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
