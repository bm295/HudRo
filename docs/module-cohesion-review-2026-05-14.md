# Cohesion Review: Data đi cùng Behavior (2026-05-14)

## Kết luận nhanh
Repo hiện tại **đạt mức khá tốt** theo tiêu chí "data và behavior đi cùng nhau" ở các flow đang có (checkout, kitchen workflow, inventory deduction). Tuy nhiên, vẫn có một vài điểm cần theo dõi khi mở rộng nghiệp vụ (đặc biệt nếu thêm loyalty/reward).

## 1) Data + behavior có đang nằm gần nhau không?

### Order lifecycle + kitchen workflow
- `Order` domain object giữ data (`Status`, `Items`, `Guests`, `TableId`) và behavior trạng thái (`AddItem`, `RemoveItem`, `SendToKitchen`, `MarkPaid`, `Close`) ngay trong cùng model.
- Rule chuyển trạng thái được đóng gói trong `EnsureStatus`, tránh rơi vào kiểu helper rải rác.

**Đánh giá:** Tốt, cohesion cao ở aggregate `Order`.

### Checkout flow
- Checkout orchestration nằm trong `CheckoutOrderWorkflow` theo đúng vai trò điều phối (inventory check/deduct -> payment -> close order).
- Payment behavior không nằm ở utility/helper mà nằm trong `PaymentApplicationService` + `IPaymentPort`.

**Đánh giá:** Tốt ở mức application orchestration; chưa thấy dấu hiệu vỡ cohesion kiểu "logic văng sang RandomExtensions".

### Inventory deduction
- Kiểm tra khả dụng và trừ kho tập trung trong `InventoryApplicationService` qua `IInventoryPort`.
- Không thấy logic trừ kho bị phân mảnh ở nhiều chỗ không liên quan.

**Đánh giá:** Tốt, module boundary rõ.

## 2) Những điểm chưa hoàn toàn "rich behavior"
- `InventoryItem` hiện là record data-only; behavior tồn kho nằm ở adapter/service thay vì nằm trong domain inventory aggregate.
- `Payment` chưa có aggregate riêng (kiểu `Payment` entity với `Authorize/Capture/Fail/Retry`), nên một phần behavior thanh toán đang ở application service và port.

**Ý nghĩa:** chưa sai kiến trúc, nhưng nếu flow payment/retry/loyalty phức tạp dần thì nguy cơ cohesion giảm sẽ tăng.

## 3) Theo 4 vùng nghiệp vụ bạn nêu
- **checkout:** Đạt, orchestration rõ và không nhồi tất cả vào một service.
- **kitchen workflow:** Đạt ở mức hiện tại qua state transition `SendToKitchen` trong `Order`.
- **inventory deduction:** Đạt, tập trung ở inventory module.
- **loyalty/reward:** **Chưa có module chuyên trách**; nên bổ sung bounded context riêng khi implement để tránh rải logic sang checkout/payment/order.

## 4) Khuyến nghị ngắn
1. Giữ nguyên hướng hiện tại: domain rule ở entity/aggregate, orchestration ở workflow.
2. Nếu thêm retry/authorization/capture nhiều bước, tạo `Payment` aggregate thay vì dồn vào service.
3. Khi thêm loyalty/reward, tách module `Loyalty` riêng (data + policy + behavior) và chỉ tích hợp qua workflow.
