namespace DataStructures.Domain.Loyalty;

public sealed class LoyaltyAccount
{
  private readonly Dictionary<Guid, PointLedgerEntry> _ledgerEntries = new();

  public Guid Id { get; }
  public Guid CustomerId { get; }
  public int PointsBalance { get; private set; }
  public LoyaltyTier Tier { get; private set; }
  public IReadOnlyCollection<PointLedgerEntry> LedgerEntries => _ledgerEntries.Values;

  public LoyaltyAccount(Guid id, Guid customerId, int pointsBalance, LoyaltyTier tier, IEnumerable<PointLedgerEntry>? ledgerEntries = null)
  {
    if (pointsBalance < 0)
    {
      throw new ArgumentOutOfRangeException(nameof(pointsBalance), "Points balance cannot be negative.");
    }

    Id = id;
    CustomerId = customerId;
    PointsBalance = pointsBalance;
    Tier = tier;

    if (ledgerEntries is null)
    {
      return;
    }

    foreach (var ledgerEntry in ledgerEntries)
    {
      AddLedgerEntry(ledgerEntry);
    }
  }

  public void AccruePoints(Guid ledgerEntryId, Guid orderId, decimal orderTotal, DateTimeOffset occurredAtUtc, LoyaltyPointPolicy policy)
  {
    var points = policy.CalculateAccrualPoints(orderTotal);
    if (points <= 0)
    {
      return;
    }

    AddLedgerEntry(new PointLedgerEntry(ledgerEntryId, PointLedgerEntryType.Accrual, points, orderId, null, occurredAtUtc, policy.AccrualExpirationDays));
    PointsBalance += points;
    RecalculateTier(policy.TierPolicy);
  }

  public void RedeemPoints(Guid ledgerEntryId, int points, DateTimeOffset occurredAtUtc, LoyaltyPointPolicy policy)
  {
    EnsurePositivePoints(points);
    EnsureAvailablePoints(points);
    policy.ValidateRedemption(points, PointsBalance);

    AddLedgerEntry(new PointLedgerEntry(ledgerEntryId, PointLedgerEntryType.Redemption, points, null, null, occurredAtUtc, null));
    PointsBalance -= points;
    RecalculateTier(policy.TierPolicy);
  }

  public void ReverseAccrual(Guid ledgerEntryId, Guid reversedLedgerEntryId, DateTimeOffset occurredAtUtc, LoyaltyPointPolicy policy)
  {
    if (!_ledgerEntries.TryGetValue(reversedLedgerEntryId, out var existing))
    {
      throw new KeyNotFoundException($"Loyalty ledger entry not found: {reversedLedgerEntryId}");
    }

    if (existing.Type != PointLedgerEntryType.Accrual)
    {
      throw new InvalidOperationException($"Only accrual entries can be reversed. Entry={reversedLedgerEntryId}");
    }

    if (_ledgerEntries.Values.Any(t => t.ReversedLedgerEntryId == reversedLedgerEntryId))
    {
      throw new InvalidOperationException($"Loyalty ledger entry {reversedLedgerEntryId} was already reversed.");
    }

    if (PointsBalance < existing.Points)
    {
      throw new InvalidOperationException("Cannot reverse loyalty transaction because it would make points balance negative.");
    }

    AddLedgerEntry(new PointLedgerEntry(ledgerEntryId, PointLedgerEntryType.Reversal, existing.Points, existing.RelatedOrderId, reversedLedgerEntryId, occurredAtUtc, null));
    PointsBalance -= existing.Points;
    RecalculateTier(policy.TierPolicy);
  }

  public int ExpirePoints(DateTimeOffset occurredAtUtc, LoyaltyPointPolicy policy)
  {
    var expirable = _ledgerEntries.Values
      .Where(e => e.IsAccrual() && e.ExpiresAtUtc is not null && e.ExpiresAtUtc <= occurredAtUtc)
      .Sum(e => e.Points);

    if (expirable <= 0)
    {
      return 0;
    }

    var expiredPoints = Math.Min(expirable, PointsBalance);
    AddLedgerEntry(new PointLedgerEntry(Guid.NewGuid(), PointLedgerEntryType.Expiration, expiredPoints, null, null, occurredAtUtc, null));
    PointsBalance -= expiredPoints;
    RecalculateTier(policy.TierPolicy);

    return expiredPoints;
  }

  private void AddLedgerEntry(PointLedgerEntry entry)
  {
    if (!_ledgerEntries.TryAdd(entry.Id, entry))
    {
      throw new InvalidOperationException($"Duplicate loyalty ledger entry id: {entry.Id}");
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

  private void RecalculateTier(LoyaltyTierPolicy tierPolicy)
  {
    Tier = tierPolicy.Resolve(PointsBalance);
  }
}
