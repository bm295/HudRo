# Implementation Plan: Data & Behavior Cohesion (FnB)

Mục tiêu: làm repo đạt mức **fully cohesive** theo nguyên tắc *data đi cùng behavior* cho các vùng checkout, kitchen workflow, inventory deduction, loyalty/reward.

## Phase 1 — Baseline architecture guardrails
1. Tạo test kiến trúc mới trong `tests/HudRo.ArchitectureTests/`:
   - Cấm `Application.*` gọi trực tiếp `Infrastructure.*` concrete types.
   - Cấm namespace `DataStructures.Application.Order` phụ thuộc trực tiếp `IPaymentPort`/`IInventoryPort` (chỉ qua workflow service orchestration).
2. Bổ sung quy ước thư mục:
   - `src/Domain/Payments`, `src/Domain/Inventory`, `src/Domain/Loyalty`.
   - `src/Application/Workflows` chỉ orchestration, không business rules chi tiết.

## Phase 2 — Payment aggregate (core requirement)
1. Thêm aggregate `Payment` (ví dụ `src/Domain/Payments/Payment.cs`) với data + behavior:
   - Data: `PaymentId`, `OrderId`, `Amount`, `Status`, `RetryCount`, `Method`, `Reference`, `FailureReason`.
   - Behavior: `Authorize()`, `Capture()`, `Fail(reason)`, `Retry()`.
2. Thêm enum `PaymentStatus`: `Pending`, `Authorized`, `Captured`, `Failed`, `Cancelled`.
3. Di chuyển validation/payment state transition vào aggregate:
   - Chỉ aggregate quyết định khi nào được retry và giới hạn retry.
4. Tạo `IPaymentRepositoryPort` để load/save aggregate payment.
5. Refactor `PaymentApplicationService` thành orchestration:
   - Load `Order` + tạo/load `Payment`.
   - Gọi behavior trên `Payment` aggregate.
   - Gọi gateway port như side-effect adapter, sau đó commit state aggregate.

## Phase 3 — Checkout workflow hardening
1. Refactor `CheckoutOrderWorkflow` để giao tiếp qua domain-intent rõ ràng:
   - `Order.ReadyForCheckout()` (domain check)
   - `InventoryReservation`/`InventoryDeduction` domain service calls
   - `Payment.Authorize/Capture`
   - `Order.MarkPaid/Close`
2. Thêm chính sách idempotency cho checkout:
   - `CheckoutSessionId` hoặc `PaymentAttemptId` để tránh double-charge/double-deduct.
3. Thêm compensation rules rõ ràng (nếu capture fail sau deduct):
   - hoặc reserve-before-capture,
   - hoặc rollback inventory transaction theo policy.

## Phase 4 — Kitchen workflow aggregate behavior
1. Mở rộng `Order` behavior để bao phủ kitchen transitions:
   - `SendToKitchen()`, `MarkPreparing()`, `MarkServed()`, `ReopenForCorrection()` (nếu policy cho phép).
2. Thêm invariant trong aggregate:
   - Không cho remove item sau `SentToKitchen` trừ correction flow hợp lệ.
3. Nếu kitchen phức tạp, tách `KitchenTicket` aggregate:
   - Data: ticket status, station, timestamps.
   - Behavior: `Start()`, `Complete()`, `BumpBack()`.

## Phase 5 — Inventory as rich domain model
1. Chuyển từ `InventoryItem` record data-only sang aggregate/entity có behavior:
   - Data: `OnHand`, `Reserved`, `Version`.
   - Behavior: `Reserve(qty)`, `Release(qty)`, `DeductReserved(qty)`, `Adjust(delta, reason)`.
2. Đảm bảo invariants nằm trong domain object:
   - Không âm kho,
   - Không deduct vượt reserved.
3. Refactor `InventoryApplicationService` chỉ còn orchestration và transaction boundary.

## Phase 6 — Loyalty/Reward bounded context
1. Tạo context riêng `src/Domain/Loyalty` + `src/Application/UseCases/LoyaltyApplicationService.cs`.
2. Thêm aggregate `LoyaltyAccount`:
   - Data: `PointsBalance`, `Tier`, `PendingPoints`.
   - Behavior: `Accrue(order)`, `Redeem(points)`, `Reverse(transaction)`.
3. Checkout chỉ publish/trigger loyalty intent; không embed rule loyalty vào order/payment service.
4. Add `ILoyaltyPort` + adapter implementation.

## Phase 7 — Consistency & integration strategy
1. **Chốt strategy nhất quán liên context (rõ ràng, áp dụng mặc định):**
   - **Mỗi aggregate = 1 transaction boundary độc lập** (không distributed transaction xuyên context).
   - **Liên context dùng eventual consistency qua Outbox/Event Log** (publish-after-commit).
   - Luồng chuẩn cho checkout: `PaymentCaptured` -> `InventoryDeducted` -> `OrderClosed` (và loyalty xử lý async ở bước sau).
2. **Event tối thiểu bắt buộc để tích hợp context:**
   - `PaymentCaptured`
   - `InventoryDeducted`
   - `OrderClosed`
3. **Quy ước vận hành Outbox:**
   - Ghi event vào outbox **trong cùng transaction** với aggregate state change.
   - Dispatcher nền đọc outbox, publish ra broker, đánh dấu đã phát.
   - Consumer phải idempotent theo `EventId`/`AggregateId` để chịu được at-least-once delivery.

## Phase 8 — Test matrix (bắt buộc để giữ cohesion)
1. Unit tests domain-first:
   - `Payment` state machine transitions và retry policy.
   - `Inventory` reserve/deduct/release invariants.
   - `Order` + kitchen transitions.
   - `LoyaltyAccount` accrue/redeem/reverse.
2. Workflow tests:
   - checkout success/failure/retry/idempotency.
3. Architecture tests:
   - cấm utility/static extensions chứa domain rules.
   - cấm background service tự thay đổi payment status mà không qua aggregate methods.

## Deliverable roadmap (ưu tiên)
1. **Sprint 1:** Payment aggregate + tests + checkout refactor tối thiểu.
2. **Sprint 2:** Inventory rich model + compensation/idempotency.
3. **Sprint 3:** Kitchen advanced transitions.
4. **Sprint 4:** Loyalty context + integration events.

## Definition of Done
- Mỗi bounded context có aggregate root rõ ràng với data + behavior colocated.
- Application layer chủ yếu orchestration, không giữ business invariants cốt lõi.
- Không còn rule nghiệp vụ nằm rải trong helper/extension/background jobs.
- Có test kiến trúc + unit tests state machine để chống regression cohesion.
