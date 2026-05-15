namespace DataStructures.Domain.Loyalty;

public sealed class LoyaltyPointPolicy
{
  public int CurrencyUnitsPerPoint { get; }
  public int MinimumRedeemPoints { get; }
  public int? AccrualExpirationDays { get; }
  public LoyaltyTierPolicy TierPolicy { get; }

  public LoyaltyPointPolicy(int currencyUnitsPerPoint, int minimumRedeemPoints, int? accrualExpirationDays, LoyaltyTierPolicy tierPolicy)
  {
    if (currencyUnitsPerPoint <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(currencyUnitsPerPoint));
    }

    if (minimumRedeemPoints <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(minimumRedeemPoints));
    }

    CurrencyUnitsPerPoint = currencyUnitsPerPoint;
    MinimumRedeemPoints = minimumRedeemPoints;
    AccrualExpirationDays = accrualExpirationDays;
    TierPolicy = tierPolicy;
  }

  public int CalculateAccrualPoints(decimal orderTotal)
  {
    if (orderTotal <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(orderTotal), "Order total must be greater than zero.");
    }

    return Convert.ToInt32(Math.Floor(orderTotal / CurrencyUnitsPerPoint));
  }

  public void ValidateRedemption(int points, int availableBalance)
  {
    if (points < MinimumRedeemPoints)
    {
      throw new InvalidOperationException($"Minimum redeem points is {MinimumRedeemPoints}.");
    }

    if (points > availableBalance)
    {
      throw new InvalidOperationException($"Insufficient points. Requested={points}, Available={availableBalance}");
    }
  }

  public static LoyaltyPointPolicy Default() => new(10_000, 100, 365, LoyaltyTierPolicy.Default());
}

public enum LoyaltyTier
{
  Bronze = 0,
  Silver = 1,
  Gold = 2,
}

public sealed class LoyaltyTierPolicy
{
  private readonly int _silverThreshold;
  private readonly int _goldThreshold;

  public LoyaltyTierPolicy(int silverThreshold, int goldThreshold)
  {
    if (silverThreshold <= 0 || goldThreshold <= silverThreshold)
    {
      throw new ArgumentOutOfRangeException(nameof(silverThreshold), "Tier thresholds are invalid.");
    }

    _silverThreshold = silverThreshold;
    _goldThreshold = goldThreshold;
  }

  public LoyaltyTier Resolve(int pointsBalance)
  {
    return pointsBalance switch
    {
      >= var p when p >= _goldThreshold => LoyaltyTier.Gold,
      >= var p when p >= _silverThreshold => LoyaltyTier.Silver,
      _ => LoyaltyTier.Bronze,
    };
  }

  public static LoyaltyTierPolicy Default() => new(1_000, 5_000);
}
