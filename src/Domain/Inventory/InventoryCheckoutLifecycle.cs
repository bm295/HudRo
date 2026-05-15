namespace DataStructures.Domain.Inventory;

public enum InventoryCheckoutState
{
  NotTouched = 0,
  Reserved = 1,
  Deducted = 2,
}

public sealed class InventoryCheckoutLifecycle
{
  public InventoryCheckoutState State { get; private set; } = InventoryCheckoutState.NotTouched;

  public bool IsReservationRequired()
    => State == InventoryCheckoutState.NotTouched;

  public bool IsDeductionRequired()
    => State != InventoryCheckoutState.Deducted;

  public void MarkReserved()
  {
    if (State != InventoryCheckoutState.NotTouched)
    {
      return;
    }

    State = InventoryCheckoutState.Reserved;
  }

  public void MarkDeducted()
  {
    if (State == InventoryCheckoutState.Deducted)
    {
      return;
    }

    if (State != InventoryCheckoutState.Reserved)
    {
      throw new InvalidOperationException("Cannot deduct inventory before reservation is completed.");
    }

    State = InventoryCheckoutState.Deducted;
  }

  public bool ShouldReleaseOnFailureWithoutPayment()
    => State == InventoryCheckoutState.Reserved;

  public void MarkReleased()
  {
    if (State == InventoryCheckoutState.Reserved)
    {
      State = InventoryCheckoutState.NotTouched;
    }
  }
}
