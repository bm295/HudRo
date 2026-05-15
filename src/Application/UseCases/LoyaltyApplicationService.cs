using DataStructures.Application.Models;
using DataStructures.Application.Ports;
using DataStructures.Domain.Loyalty;

namespace DataStructures.Application.Loyalty;

public sealed class LoyaltyApplicationService(ILoyaltyAccountPort loyaltyAccountPort)
{
  public async Task AccrueAsync(LoyaltyAccrualIntent intent, CancellationToken cancellationToken = default)
  {
    var account = await loyaltyAccountPort.FindByCustomerIdAsync(intent.CustomerId, cancellationToken)
      ?? new LoyaltyAccount(Guid.NewGuid(), intent.CustomerId, 0, LoyaltyTier.Bronze);

    account.Accrue(intent.IntentId, intent.OrderId, intent.OrderTotal, intent.OccurredAtUtc);

    await loyaltyAccountPort.SaveAsync(account, cancellationToken);
  }
}
