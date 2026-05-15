using DataStructures.Application.Models;
using DataStructures.Application.Ports;
using DataStructures.Domain.Loyalty;

namespace DataStructures.Application.Loyalty;

public sealed class LoyaltyApplicationService(ILoyaltyAccountPort loyaltyAccountPort) : ILoyaltyOperations
{
  private static readonly LoyaltyPointPolicy Policy = LoyaltyPointPolicy.Default();

  public async Task AccrueAsync(LoyaltyAccrualIntent intent, CancellationToken cancellationToken = default)
  {
    var account = await loyaltyAccountPort.FindByCustomerIdAsync(intent.CustomerId, cancellationToken)
      ?? new LoyaltyAccount(Guid.NewGuid(), intent.CustomerId, 0, LoyaltyTier.Bronze);

    account.AccruePoints(intent.IntentId, intent.OrderId, intent.OrderTotal, intent.OccurredAtUtc, Policy);

    await loyaltyAccountPort.SaveAsync(account, cancellationToken);
  }

  public async Task RedeemAsync(LoyaltyRedeemIntent intent, CancellationToken cancellationToken = default)
  {
    var account = await loyaltyAccountPort.FindByCustomerIdAsync(intent.CustomerId, cancellationToken)
      ?? throw new InvalidOperationException($"Loyalty account was not found for customer {intent.CustomerId}.");

    account.RedeemPoints(intent.IntentId, intent.Points, intent.OccurredAtUtc, Policy);

    await loyaltyAccountPort.SaveAsync(account, cancellationToken);
  }

  public async Task ReverseAccrualAsync(LoyaltyReverseAccrualIntent intent, CancellationToken cancellationToken = default)
  {
    var account = await loyaltyAccountPort.FindByCustomerIdAsync(intent.CustomerId, cancellationToken)
      ?? throw new InvalidOperationException($"Loyalty account was not found for customer {intent.CustomerId}.");

    account.ReverseAccrual(intent.IntentId, intent.ReversedLedgerEntryId, intent.OccurredAtUtc, Policy);

    await loyaltyAccountPort.SaveAsync(account, cancellationToken);
  }

  public async Task<int> ExpirePointsAsync(LoyaltyExpirePointsIntent intent, CancellationToken cancellationToken = default)
  {
    var account = await loyaltyAccountPort.FindByCustomerIdAsync(intent.CustomerId, cancellationToken)
      ?? throw new InvalidOperationException($"Loyalty account was not found for customer {intent.CustomerId}.");

    var expiredPoints = account.ExpirePoints(intent.OccurredAtUtc, Policy);

    await loyaltyAccountPort.SaveAsync(account, cancellationToken);

    return expiredPoints;
  }
}
