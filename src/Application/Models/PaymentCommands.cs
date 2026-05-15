using DataStructures.Domain;

namespace DataStructures.Application.Models;

public sealed record ProcessPaymentCommand(Guid OrderId, PaymentMethod Method);

public sealed record CheckoutOrderCommand(
  Guid OrderId,
  PaymentMethod Method,
  Guid CheckoutSessionId,
  Guid PaymentAttemptId,
  Guid CustomerId);
