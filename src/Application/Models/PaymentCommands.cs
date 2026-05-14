using DataStructures.Domain;

namespace DataStructures.Application.Models;

public sealed record ProcessPaymentCommand(Guid OrderId, PaymentMethod Method);
