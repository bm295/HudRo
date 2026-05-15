namespace DataStructures.Domain;

public sealed record RestaurantProfile(string Name, int MinSeats, int MaxSeats);

public sealed record DiningTable(string Id, int Seats);

public sealed record MenuItem(string Code, string Name, decimal Price);

public enum OrderStatus
{
  Draft = 0,
  SentToKitchen = 1,
  Preparing = 2,
  Served = 3,
  Paid = 4,
  Closed = 5,
}

public enum PaymentMethod
{
  Cash = 0,
  Card = 1,
  BankTransfer = 2,
}

public sealed class InventoryItem
{
  public string Sku { get; }
  public string Name { get; }
  public int QuantityOnHand { get; private set; }
  public int QuantityReserved { get; private set; }

  public InventoryItem(string sku, string name, int quantityOnHand, int quantityReserved = 0)
  {
    if (string.IsNullOrWhiteSpace(sku))
    {
      throw new ArgumentException("SKU is required.", nameof(sku));
    }

    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Name is required.", nameof(name));
    }

    if (quantityOnHand < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(quantityOnHand), "Quantity on hand cannot be negative.");
    }

    if (quantityReserved < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(quantityReserved), "Quantity reserved cannot be negative.");
    }

    if (quantityReserved > quantityOnHand)
    {
      throw new InvalidOperationException("Reserved quantity cannot exceed quantity on hand.");
    }

    Sku = sku;
    Name = name;
    QuantityOnHand = quantityOnHand;
    QuantityReserved = quantityReserved;
  }

  public void Reserve(int quantity)
  {
    ValidatePositive(quantity);

    var available = QuantityOnHand - QuantityReserved;
    if (available < quantity)
    {
      throw new InvalidOperationException($"Not enough stock for {Sku}. Requested={quantity}, Available={available}");
    }

    QuantityReserved += quantity;
  }

  public void Release(int quantity)
  {
    ValidatePositive(quantity);

    if (QuantityReserved < quantity)
    {
      throw new InvalidOperationException($"Cannot release {quantity} of {Sku}; only {QuantityReserved} reserved.");
    }

    QuantityReserved -= quantity;
  }

  public void DeductReserved(int quantity)
  {
    ValidatePositive(quantity);

    if (QuantityReserved < quantity)
    {
      throw new InvalidOperationException($"Cannot deduct {quantity} of {Sku}; only {QuantityReserved} reserved.");
    }

    QuantityReserved -= quantity;
    QuantityOnHand -= quantity;
  }

  public void Adjust(int delta)
  {
    var updatedOnHand = QuantityOnHand + delta;
    if (updatedOnHand < 0)
    {
      throw new InvalidOperationException($"Adjustment would make stock negative for {Sku}.");
    }

    if (updatedOnHand < QuantityReserved)
    {
      throw new InvalidOperationException(
        $"Adjustment would make on-hand lower than reserved for {Sku}. OnHand={updatedOnHand}, Reserved={QuantityReserved}");
    }

    QuantityOnHand = updatedOnHand;
  }

  private static void ValidatePositive(int quantity)
  {
    if (quantity <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
    }
  }
}

public sealed record OrderItem(string MenuCode, int Quantity)
{
  public static OrderItem Create(string menuCode, int quantity)
  {
    if (string.IsNullOrWhiteSpace(menuCode))
    {
      throw new ArgumentException("Menu code is required.", nameof(menuCode));
    }

    if (quantity <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
    }

    return new OrderItem(menuCode, quantity);
  }
}

public sealed class Order
{
  private readonly Dictionary<string, int> _items = new(StringComparer.Ordinal);

  public Guid Id { get; }
  public string TableId { get; }
  public int Guests { get; }
  public OrderStatus Status { get; private set; }
  public DateTimeOffset OpenedAtUtc { get; }

  public IReadOnlyDictionary<string, int> Items => _items;

  public Order(Guid id, string tableId, int guests, DateTimeOffset openedAtUtc)
  {
    if (string.IsNullOrWhiteSpace(tableId))
    {
      throw new ArgumentException("Table id is required.", nameof(tableId));
    }

    if (guests <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(guests), "Guests must be greater than zero.");
    }

    Id = id;
    TableId = tableId;
    Guests = guests;
    OpenedAtUtc = openedAtUtc;
    Status = OrderStatus.Draft;
  }

  public void AddItem(OrderItem item)
  {
    EnsureStatus(OrderStatus.Draft);
    _items[item.MenuCode] = _items.GetValueOrDefault(item.MenuCode) + item.Quantity;
  }

  public void RemoveItem(string menuCode, int quantity)
  {
    EnsureStatus(OrderStatus.Draft);

    if (!_items.TryGetValue(menuCode, out var current))
    {
      throw new InvalidOperationException($"Item {menuCode} is not in the order.");
    }

    if (quantity <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
    }

    var updated = current - quantity;
    if (updated < 0)
    {
      throw new InvalidOperationException($"Cannot remove {quantity} of {menuCode}; only {current} in order.");
    }

    if (updated == 0)
    {
      _items.Remove(menuCode);
      return;
    }

    _items[menuCode] = updated;
  }

  public void SendToKitchen()
  {
    EnsureStatus(OrderStatus.Draft);

    if (_items.Count == 0)
    {
      throw new InvalidOperationException("Cannot send empty order to kitchen.");
    }

    Status = OrderStatus.SentToKitchen;
  }

  public void MarkPaid()
  {
    EnsureStatus(OrderStatus.Served);
    Status = OrderStatus.Paid;
  }

  public void MarkPreparing()
  {
    EnsureStatus(OrderStatus.SentToKitchen);
    Status = OrderStatus.Preparing;
  }

  public void MarkServed()
  {
    EnsureStatus(OrderStatus.Preparing);
    Status = OrderStatus.Served;
  }

  public void Close()
  {
    EnsureStatus(OrderStatus.Paid);
    Status = OrderStatus.Closed;
  }

  public void EnsureReadyForCheckout()
  {
    EnsureStatus(OrderStatus.Served);

    if (_items.Count == 0)
    {
      throw new InvalidOperationException($"Order {Id} has no items and cannot be checked out.");
    }
  }

  private void EnsureStatus(OrderStatus expected)
  {
    if (Status != expected)
    {
      throw new InvalidOperationException($"Order {Id} must be {expected} but is {Status}.");
    }
  }
}

public sealed record BillLine(string ItemName, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record TableBill(
  Guid OrderId,
  string TableId,
  int Guests,
  IReadOnlyList<BillLine> Lines,
  decimal Total,
  PaymentMethod PaymentMethod,
  string PaymentReference);

public sealed record ServiceSummary(
  RestaurantProfile Profile,
  int ConfiguredSeats,
  int ServedGuests,
  int OrdersClosed,
  decimal Revenue,
  IReadOnlyList<TableBill> Bills);
