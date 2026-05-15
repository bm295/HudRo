namespace DataStructures.Domain.Loyalty;

public enum LoyaltyTier
{
  Bronze = 0,
  Silver = 1,
  Gold = 2,
}

public enum LoyaltyTransactionType
{
  Accrual = 0,
  Redemption = 1,
  Reversal = 2,
}

public sealed record LoyaltyTransaction(
  Guid Id,
  LoyaltyTransactionType Type,
  int Points,
  Guid? RelatedOrderId,
  Guid? ReversedTransactionId,
  DateTimeOffset OccurredAtUtc);

public sealed class LoyaltyAccount
{
  private readonly Dictionary<Guid, LoyaltyTransaction> _transactions = new();

  public Guid Id { get; }
  public Guid CustomerId { get; }
  public int PointsBalance { get; private set; }
  public LoyaltyTier Tier { get; private set; }
  public IReadOnlyCollection<LoyaltyTransaction> Transactions => _transactions.Values;

  public LoyaltyAccount(Guid id, Guid customerId, int pointsBalance, LoyaltyTier tier)
  {
    if (pointsBalance < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(pointsBalance), "Points balance cannot be negative.");
    }

    Id = id;
    CustomerId = customerId;
    PointsBalance = pointsBalance;
    Tier = tier;
  }

  public void Accrue(Guid transactionId, Guid orderId, decimal orderTotal, DateTimeOffset occurredAtUtc)
  {
    if (orderTotal <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(orderTotal), "Order total must be greater than zero.");
    }

    var points = Convert.ToInt32(Math.Floor(orderTotal / 10_000m));
    if (points <= 0)
    {
      return;
    }

    AddTransaction(new LoyaltyTransaction(transactionId, LoyaltyTransactionType.Accrual, points, orderId, null, occurredAtUtc));
    PointsBalance += points;
    RecalculateTier();
  }

  public void Redeem(Guid transactionId, int points, DateTimeOffset occurredAtUtc)
  {
    EnsurePositivePoints(points);
    EnsureAvailablePoints(points);

    AddTransaction(new LoyaltyTransaction(transactionId, LoyaltyTransactionType.Redemption, points, null, null, occurredAtUtc));
    PointsBalance -= points;
    RecalculateTier();
  }

  public void Reverse(Guid transactionId, Guid reversedTransactionId, DateTimeOffset occurredAtUtc)
  {
    if (!_transactions.TryGetValue(reversedTransactionId, out var existing))
    {
      throw new KeyNotFoundException($"Loyalty transaction not found: {reversedTransactionId}");
    }

    if (_transactions.Values.Any(t => t.ReversedTransactionId == reversedTransactionId))
    {
      throw new InvalidOperationException($"Loyalty transaction {reversedTransactionId} was already reversed.");
    }

    var adjustment = existing.Type == LoyaltyTransactionType.Accrual ? -existing.Points : existing.Points;
    if (PointsBalance + adjustment < 0)
    {
      throw new InvalidOperationException("Cannot reverse loyalty transaction because it would make points balance negative.");
    }

    AddTransaction(new LoyaltyTransaction(transactionId, LoyaltyTransactionType.Reversal, Math.Abs(existing.Points), existing.RelatedOrderId, reversedTransactionId, occurredAtUtc));
    PointsBalance += adjustment;
    RecalculateTier();
  }

  private void AddTransaction(LoyaltyTransaction transaction)
  {
    if (!_transactions.TryAdd(transaction.Id, transaction))
    {
      throw new InvalidOperationException($"Duplicate loyalty transaction id: {transaction.Id}");
    }
  }

  private static void EnsurePositivePoints(int points)
  {
    if (points <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(points), "Points must be greater than zero.");
    }
  }

  private void EnsureAvailablePoints(int points)
  {
    if (PointsBalance < points)
    {
      throw new InvalidOperationException($"Insufficient points. Requested={points}, Available={PointsBalance}");
    }
  }

  private void RecalculateTier()
  {
    Tier = PointsBalance switch
    {
      >= 5_000 => LoyaltyTier.Gold,
      >= 1_000 => LoyaltyTier.Silver,
      _ => LoyaltyTier.Bronze,
    };
  }
}
