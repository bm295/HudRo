# Repository Architecture Review — HudRo FnB Management

This file is retained as implementation status after refactoring.

## Verdict

**PASS (for the required scope in this repository sample)**

## Notes

- Hexagonal layers are now explicit: Domain, Application (use cases + ports + commands), Infrastructure adapters, Console adapter.
- The operational flow is implemented end-to-end:
 1. Create order for a table
 2. Add / remove items
 3. Send order to kitchen
 4. Process payment
 5. Deduct inventory
 6. Close order
- Dependency injection is configured in `Program.cs` using `ServiceCollection`.
- Ports and use cases are asynchronous (`Task`) to support non-blocking integrations.

## Business Rule Mapping (FnB -> Modules)

- Order lifecycle rule (`Draft -> SentToKitchen -> Paid -> Closed`)
  - **Module:** Order
  - **Current placement:** Domain model + `OrderApplicationService`
- Add/remove item before kitchen handoff
  - **Module:** Order
  - **Current placement:** Domain model + Order commands/service
- Kitchen handoff transition
  - **Module:** Order
  - **Current placement:** Order service/workflow
- Payment authorization/capture
  - **Module:** Payment
  - **Current placement:** `PaymentApplicationService` via `IPaymentPort`
- Inventory deduction after successful payment
  - **Module:** Inventory
  - **Current placement:** `InventoryApplicationService` via `IInventoryPort`
- Operational summaries (end-of-shift / totals / snapshots)
  - **Module:** Reporting
  - **Current placement:** `ReportingApplicationService` via `IFnbReadPort`

## Change Scenarios

1. **Change payment gateway provider**
   - Expected impact: only Payment module implementation details (`IPaymentPort` adapter and Payment wiring).
   - No business-rule changes required in Order/Inventory/Reporting modules.

2. **Change seating rule (e.g., table capacity constraints)**
   - Expected impact: specific Order rule(s) validating seating during order creation/update.
   - Reporting may need targeted rule/query update only when seating KPI definitions change.
   - Payment and Inventory modules remain unaffected.

## Cohesion and Boundary Principle

- Do **not** add cross-concern methods into a service that owns another concern.
  - Example anti-pattern: adding reporting behavior into Payment service.
  - Example anti-pattern: embedding payment gateway calls directly inside Order service.
- Keep each application service aligned to one primary reason-to-change (Order, Payment, Inventory, or Reporting).
