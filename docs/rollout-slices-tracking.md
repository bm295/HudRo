# Rollout slices tracking

## Slice 1: Payment aggregate + tests
- Added payment aggregate state-machine tests to verify bounded retry transitions are explicit in-domain (no helper-based validation fallback).
- Removed anti-pattern: **retry in background logic** by making retry trigger explicit in `PaymentApplicationService` and relying on aggregate guard clauses.

## Slice 2: Checkout orchestration cleanup
- Added idempotency workflow test to verify a replay does not re-run inventory/payment/loyalty side effects.
- Removed anti-pattern: **cross-module state mutation** by keeping replay-safe orchestration state in `CheckoutOrderWorkflow` and avoiding duplicate calls into ports on same checkout session.

## Slice 3: Inventory consolidation
- Existing inventory lifecycle keeps reserve/deduct/release transitions in domain/application boundary with invariant-protected operations.
- Removed anti-pattern: **helper-based validation** by relying on domain invariants (`InventoryItem`, `InventoryCheckoutLifecycle`) instead of external helper validators.

## Slice 4: Loyalty bounded context introduction
- Loyalty operations remain isolated behind `ILoyaltyOperations` + `LoyaltyApplicationService` with dedicated domain (`LoyaltyAccount`, ledger entries, policies).
- Compatibility retained through in-memory adapter while orchestration triggers intent only after close-order parity checks.
