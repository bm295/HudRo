using DataStructures.Application.Models;

namespace DataStructures.Application.Ports;

public interface ILoyaltyOperations
{
  Task AccrueAsync(LoyaltyAccrualIntent intent, CancellationToken cancellationToken = default);
  Task RedeemAsync(LoyaltyRedeemIntent intent, CancellationToken cancellationToken = default);
  Task ReverseAccrualAsync(LoyaltyReverseAccrualIntent intent, CancellationToken cancellationToken = default);
  Task<int> ExpirePointsAsync(LoyaltyExpirePointsIntent intent, CancellationToken cancellationToken = default);
}
