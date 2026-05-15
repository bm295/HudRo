using DataStructures.Domain.Loyalty;

namespace DataStructures.Application.Ports;

public interface ILoyaltyAccountPort
{
  Task<LoyaltyAccount?> FindByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);
  Task SaveAsync(LoyaltyAccount account, CancellationToken cancellationToken);
}
