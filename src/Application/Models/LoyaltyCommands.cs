namespace DataStructures.Application.Models;

public sealed record LoyaltyAccrualIntent(Guid OrderId, Guid CustomerId, decimal OrderTotal, Guid IntentId, DateTimeOffset OccurredAtUtc);
