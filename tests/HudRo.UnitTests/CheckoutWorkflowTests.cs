using DataStructures.Application.Inventory;
using DataStructures.Application.Loyalty;
using DataStructures.Application.Models;
using DataStructures.Application.Order;
using DataStructures.Application.Payment;
using DataStructures.Application.Ports;
using DataStructures.Application.Workflows;
using DataStructures.Domain;
using DataStructures.Domain.Loyalty;
using DataStructures.Domain.Payments;
using Xunit;

namespace HudRo.UnitTests;

public sealed class CheckoutWorkflowTests
{
  [Fact]
  public async Task Checkout_ShouldHandleSuccessRetryAndIdempotency()
  {
    var order = BuildServedOrder();
    var menu = new Dictionary<string, MenuItem> { ["M1"] = new("M1", "Pho", 50_000m) };

    var readPort = new FakeReadPort(menu);
    var orderPort = new FakeOrderPort(order);
    var inventoryPort = new FakeInventoryPort();
    var paymentAggregatePort = new FakePaymentAggregatePort();
    var paymentGatewayPort = new FlakyGatewayPort();
    var loyaltyPort = new FakeLoyaltyPort();

    var workflow = new CheckoutOrderWorkflow(
      new OrderApplicationService(readPort, orderPort),
      new InventoryApplicationService(inventoryPort),
      new PaymentApplicationService(readPort, orderPort, paymentAggregatePort, paymentGatewayPort),
      new LoyaltyApplicationService(loyaltyPort));

    var cmd = new CheckoutOrderCommand(Guid.NewGuid(), order.Id, PaymentMethod.Card, Guid.NewGuid(), Guid.NewGuid());

    await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.ExecuteAsync(cmd));
    Assert.Equal(1, inventoryPort.ReserveCalls);
    Assert.Equal(1, inventoryPort.ReleaseCalls);

    var result = await workflow.ExecuteAsync(cmd);
    Assert.Equal(OrderStatus.Closed, order.Status);
    Assert.Equal(PaymentStatus.Captured, result.PaymentStatus);

    var second = await workflow.ExecuteAsync(cmd);
    Assert.Equal(result.Bill.PaymentReference, second.Bill.PaymentReference);
    Assert.Equal(2, inventoryPort.ReserveCalls); // first failed + one successful; idempotent replay should not reserve again
    Assert.Equal(1, paymentGatewayPort.CallCount); // only successful charge once for session
  }

  private static Order BuildServedOrder()
  {
    var order = new Order(Guid.NewGuid(), "T1", 2, DateTimeOffset.UtcNow);
    order.AddItem(OrderItem.Create("M1", 1));
    order.SendToKitchen();
    order.MarkPreparing();
    order.MarkServed();
    return order;
  }

  private sealed class FakeReadPort(IReadOnlyDictionary<string, MenuItem> menu) : IFnbReadPort
  {
    public Task<IReadOnlyList<DiningTable>> GetTablesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DiningTable>>([]);
    public Task<IReadOnlyDictionary<string, MenuItem>> GetMenuAsync(CancellationToken cancellationToken = default) => Task.FromResult(menu);
    public Task<RestaurantProfile> GetRestaurantProfileAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RestaurantProfile("R", 1, 10));
  }

  private sealed class FakeOrderPort(Order order) : IOrderPort
  {
    public Task<Order?> FindOpenOrderByTableAsync(string tableId, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);
    public Task<Order?> FindByIdAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(order);
    public Task SaveAsync(Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IReadOnlyList<Order>> GetOpenOrdersAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Order>>([order]);
  }

  private sealed class FakeInventoryPort : IInventoryPort
  {
    public int ReserveCalls { get; private set; }
    public int DeductCalls { get; private set; }
    public int ReleaseCalls { get; private set; }
    public Task ReserveAsync(string sku, int quantity, CancellationToken cancellationToken = default) { ReserveCalls++; return Task.CompletedTask; }
    public Task DeductReservedAsync(string sku, int quantity, CancellationToken cancellationToken = default) { DeductCalls++; return Task.CompletedTask; }
    public Task ReleaseAsync(string sku, int quantity, CancellationToken cancellationToken = default) { ReleaseCalls++; return Task.CompletedTask; }
  }

  private sealed class FakePaymentAggregatePort : IPaymentAggregatePort
  {
    private Payment? _payment;
    public Task<Payment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.FromResult(_payment);
    public Task SaveAsync(Payment payment, CancellationToken cancellationToken = default) { _payment = payment; return Task.CompletedTask; }
  }

  private sealed class FlakyGatewayPort : IPaymentGatewayPort
  {
    public int CallCount { get; private set; }
    public Task<string> ChargeAsync(Guid orderId, decimal amount, PaymentMethod method, Guid paymentAttemptId, CancellationToken cancellationToken = default)
    {
      CallCount++;
      if (CallCount == 1) throw new InvalidOperationException("gateway-down");
      return Task.FromResult("PAY-REF-1");
    }
  }

  private sealed class FakeLoyaltyPort : ILoyaltyAccountPort
  {
    public Task<LoyaltyAccount?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default) => Task.FromResult<LoyaltyAccount?>(null);
    public Task SaveAsync(LoyaltyAccount account, CancellationToken cancellationToken = default) => Task.CompletedTask;
  }
}
