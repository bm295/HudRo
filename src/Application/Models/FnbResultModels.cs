using DataStructures.Domain;
using DataStructures.Domain.Payments;

namespace DataStructures.Application.Models;

public sealed record PaymentResult(
  Guid OrderId,
  decimal Amount,
  PaymentMethod Method,
  string PaymentReference,
  PaymentStatus Status,
  int RetryCount,
  string? FailureReason);

public sealed record CloseOrderResult(TableBill Bill);

public sealed record ServiceSummaryResult(
  RestaurantProfile Profile,
  int ConfiguredSeats,
  int ServedGuests,
  int OrdersClosed,
  decimal Revenue,
  IReadOnlyList<TableBill> Bills);
