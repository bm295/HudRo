using DataStructures.Application.Ports;
using DataStructures.Domain.Loyalty;

namespace DataStructures.Infrastructure;

public sealed class InMemoryLoyaltyAccountAdapter(InMemoryFnbStore store) : ILoyaltyAccountPort
{
  public Task<LoyaltyAccount?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken)
  {
    store.LoyaltyAccounts.TryGetValue(customerId, out var account);
    return Task.FromResult(account);
  }

  public Task SaveAsync(LoyaltyAccount account, CancellationToken cancellationToken)
  {
    store.LoyaltyAccounts[account.CustomerId] = account;
    return Task.CompletedTask;
  }
}
