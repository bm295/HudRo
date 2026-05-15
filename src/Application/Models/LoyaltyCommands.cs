namespace DataStructures.Application.Models;

public sealed record LoyaltyAccrualIntent(Guid OrderId, Guid CustomerId, decimal OrderTotal, Guid IntentId, DateTimeOffset OccurredAtUtc);
public sealed record LoyaltyRedeemIntent(Guid CustomerId, int Points, Guid IntentId, DateTimeOffset OccurredAtUtc);
public sealed record LoyaltyReverseAccrualIntent(Guid CustomerId, Guid ReversedLedgerEntryId, Guid IntentId, DateTimeOffset OccurredAtUtc);
public sealed record LoyaltyExpirePointsIntent(Guid CustomerId, DateTimeOffset OccurredAtUtc);
