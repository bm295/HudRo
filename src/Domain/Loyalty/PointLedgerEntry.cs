namespace DataStructures.Domain.Loyalty;

public enum PointLedgerEntryType
{
  Accrual = 0,
  Redemption = 1,
  Reversal = 2,
  Expiration = 3,
}

public sealed record PointLedgerEntry(
  Guid Id,
  PointLedgerEntryType Type,
  int Points,
  Guid? RelatedOrderId,
  Guid? ReversedLedgerEntryId,
  DateTimeOffset OccurredAtUtc,
  int? ExpirationDays)
{
  public DateTimeOffset? ExpiresAtUtc => ExpirationDays is null
    ? null
    : OccurredAtUtc.AddDays(ExpirationDays.Value);

  public bool IsAccrual() => Type == PointLedgerEntryType.Accrual;
}
