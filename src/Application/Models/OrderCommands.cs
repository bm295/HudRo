namespace DataStructures.Application.Models;

public sealed record CreateOrderCommand(string TableId, int Guests);

public sealed record AddOrderItemCommand(Guid OrderId, string MenuCode, int Quantity);

public sealed record RemoveOrderItemCommand(Guid OrderId, string MenuCode, int Quantity);

public sealed record SendOrderToKitchenCommand(Guid OrderId);
