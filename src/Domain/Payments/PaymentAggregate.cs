namespace DataStructures.Domain.Payments;

public enum PaymentStatus
{
  Pending = 0,
  Authorized = 1,
  Captured = 2,
  Failed = 3,
  Cancelled = 4,
}

public sealed class Payment
{
  public const int MaxRetryCount = 3;

  public Guid PaymentId { get; }
  public Guid OrderId { get; }
  public decimal Amount { get; }
  public PaymentMethod Method { get; }
  public PaymentStatus Status { get; private set; }
  public int RetryCount { get; private set; }
  public string? Reference { get; private set; }
  public string? FailureReason { get; private set; }
  public DateTimeOffset CreatedAt { get; }
  public DateTimeOffset UpdatedAt { get; private set; }
  public DateTimeOffset? AuthorizedAt { get; private set; }
  public DateTimeOffset? CapturedAt { get; private set; }
  public DateTimeOffset? FailedAt { get; private set; }
  public DateTimeOffset? LastRetriedAt { get; private set; }

  public Guid Id => PaymentId;

  private Payment(Guid paymentId, Guid orderId, decimal amount, PaymentMethod method, DateTimeOffset createdAt)
  {
    if (paymentId == Guid.Empty)
    {
      throw new ArgumentException("Payment id is required.", nameof(paymentId));
    }

    if (orderId == Guid.Empty)
    {
      throw new ArgumentException("Order id is required.", nameof(orderId));
    }

    if (amount <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
    }

    PaymentId = paymentId;
    OrderId = orderId;
    Amount = amount;
    Method = method;
    Status = PaymentStatus.Pending;
    CreatedAt = createdAt;
    UpdatedAt = createdAt;
  }

  public static Payment CreateNew(Guid orderId, decimal amount, PaymentMethod method)
    => new(Guid.NewGuid(), orderId, amount, method, DateTimeOffset.UtcNow);

  public bool NeedsRetry()
    => Status == PaymentStatus.Failed && RetryCount < MaxRetryCount;

  public void Authorize(string reference, DateTimeOffset? authorizedAt = null)
  {
    if (string.IsNullOrWhiteSpace(reference))
    {
      throw new ArgumentException("Payment reference is required.", nameof(reference));
    }

    EnsureStatus(PaymentStatus.Pending);

    Reference = reference;
    FailureReason = null;
    Status = PaymentStatus.Authorized;
    AuthorizedAt = authorizedAt ?? DateTimeOffset.UtcNow;
    Touch();
  }

  public void Capture(DateTimeOffset? capturedAt = null)
  {
    EnsureStatus(PaymentStatus.Authorized);
    Status = PaymentStatus.Captured;
    CapturedAt = capturedAt ?? DateTimeOffset.UtcNow;
    Touch();
  }

  public void Fail(string reason, DateTimeOffset? failedAt = null)
  {
    if (string.IsNullOrWhiteSpace(reason))
    {
      throw new ArgumentException("Failure reason is required.", nameof(reason));
    }

    if (Status is PaymentStatus.Captured)
    {
      throw new InvalidOperationException($"Payment {PaymentId} is already captured and cannot fail.");
    }

    FailureReason = reason;
    Status = PaymentStatus.Failed;
    FailedAt = failedAt ?? DateTimeOffset.UtcNow;
    Touch();
  }

  public void Retry(DateTimeOffset? retriedAt = null)
  {
    if (!NeedsRetry())
    {
      throw new InvalidOperationException($"Payment {PaymentId} cannot retry. Status={Status}, RetryCount={RetryCount}.");
    }

    RetryCount++;
    Status = PaymentStatus.Pending;
    FailureReason = null;
    Reference = null;
    LastRetriedAt = retriedAt ?? DateTimeOffset.UtcNow;
    Touch();
  }

  public bool CanRetry()
    => NeedsRetry();

  private void EnsureStatus(PaymentStatus expected)
  {
    if (Status != expected)
    {
      throw new InvalidOperationException($"Payment {PaymentId} must be {expected} but is {Status}.");
    }
  }

  private void Touch()
  {
    UpdatedAt = DateTimeOffset.UtcNow;
  }
}
