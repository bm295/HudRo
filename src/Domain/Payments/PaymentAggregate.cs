namespace DataStructures.Domain.Payments;

public enum PaymentStatus
{
  Pending = 0,
  Authorized = 1,
  Captured = 2,
  Failed = 3,
}

public sealed class Payment
{
  public const int MaxRetryCount = 3;

  public Guid Id { get; }
  public Guid OrderId { get; }
  public decimal Amount { get; }
  public PaymentMethod Method { get; }
  public PaymentStatus Status { get; private set; }
  public int RetryCount { get; private set; }
  public string? Reference { get; private set; }
  public string? FailureReason { get; private set; }

  private Payment(Guid id, Guid orderId, decimal amount, PaymentMethod method)
  {
    if (amount <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
    }

    Id = id;
    OrderId = orderId;
    Amount = amount;
    Method = method;
    Status = PaymentStatus.Pending;
  }

  public static Payment CreateNew(Guid orderId, decimal amount, PaymentMethod method)
    => new(Guid.NewGuid(), orderId, amount, method);

  public void Authorize(string reference)
  {
    if (string.IsNullOrWhiteSpace(reference))
    {
      throw new ArgumentException("Payment reference is required.", nameof(reference));
    }

    EnsureStatus(PaymentStatus.Pending);

    Reference = reference;
    FailureReason = null;
    Status = PaymentStatus.Authorized;
  }

  public void Capture()
  {
    EnsureStatus(PaymentStatus.Authorized);
    Status = PaymentStatus.Captured;
  }

  public void Fail(string reason)
  {
    if (string.IsNullOrWhiteSpace(reason))
    {
      throw new ArgumentException("Failure reason is required.", nameof(reason));
    }

    if (Status is PaymentStatus.Captured)
    {
      throw new InvalidOperationException($"Payment {Id} is already captured and cannot fail.");
    }

    FailureReason = reason;
    Status = PaymentStatus.Failed;
  }

  public void Retry()
  {
    if (!CanRetry())
    {
      throw new InvalidOperationException($"Payment {Id} cannot retry. Status={Status}, RetryCount={RetryCount}.");
    }

    RetryCount++;
    Status = PaymentStatus.Pending;
    FailureReason = null;
    Reference = null;
  }

  public bool CanRetry()
    => Status == PaymentStatus.Failed && RetryCount < MaxRetryCount;

  private void EnsureStatus(PaymentStatus expected)
  {
    if (Status != expected)
    {
      throw new InvalidOperationException($"Payment {Id} must be {expected} but is {Status}.");
    }
  }
}
