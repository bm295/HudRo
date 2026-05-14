# DataStructures

Ứng dụng mẫu quản lý FnB cho **HudRo** theo kiến trúc **Hexagonal Architecture (Ports & Adapters)**.

## Mục tiêu

- Quản lý quy mô chỗ ngồi của nhà hàng trong khoảng **40–60 chỗ**.
- Mô phỏng luồng vận hành thực tế:
 1. Create order for table
 2. Add / remove items
 3. Send order to kitchen
 4. Process payment
 5. Deduct inventory
 6. Close order

## Kiến trúc

- Sơ đồ modules và trách nhiệm:

```text
Console Adapter
    |
    v
Application Layer -----------------------------------------------------------+
| Order module      : tạo order, thêm/bớt món, gửi bếp, đóng order          |
| Payment module    : xử lý thanh toán qua payment port/gateway             |
| Inventory module  : trừ tồn kho theo món đã bán                            |
| Reporting module  : đọc dữ liệu và tổng hợp báo cáo vận hành               |
+---------------------------------------------------------------------------+
    |
    v
Ports (`IOrderPort`, `IPaymentPort`, `IInventoryPort`, `IFnbReadPort`)
    |
    v
Infrastructure Adapters (in-memory order/payment/inventory/read adapters)
    |
    v
Domain (entities + business rules cốt lõi)
```

- `src/Domain`
  - Entities/value objects: `Order`, `MenuItem`, `DiningTable`, `InventoryItem`.
  - Business rules: trạng thái order (`Draft -> SentToKitchen -> Paid -> Closed`), add/remove món, validate flow.
- `src/Application`
  - Điều phối use case theo từng module: Order, Payment, Inventory, Reporting.
  - Command/query models cho từng thao tác nghiệp vụ.
  - Ports: `IFnbReadPort`, `IOrderPort`, `IInventoryPort`, `IPaymentPort`.
- `src/Infrastructure`
  - In-memory adapters cho đọc dữ liệu, order persistence, inventory, payment gateway.
- `src/Adapters/Console`
  - Presenter hiển thị bill và báo cáo cuối ca.

### Cohesion đúng/sai (ví dụ)

- **Đúng (high cohesion):**
  - `OrderApplicationService` chỉ xử lý hành vi vòng đời order (create, add/remove item, send to kitchen, close).
  - `PaymentApplicationService` chỉ xử lý quy tắc thanh toán và gọi payment port.

- **Sai (low cohesion):**
  - Thêm method như `GenerateEndOfDayReport()` vào `PaymentApplicationService`.
  - Thêm method như `CallPaymentGateway()` vào `OrderApplicationService`.

### Nguyên tắc module boundary

- Không thêm method **cross-concern** vào cùng service.
- Mỗi service/module chỉ nên có **một nhóm reason-to-change chính**.
- Khi có rule nghiệp vụ mới, ưu tiên đặt vào module cùng concern thay vì “tiện tay” nhét vào service đang có.

### Change scenarios

- **Đổi cổng thanh toán** (ví dụ thay provider):
  - Chỉ cần thay adapter/implementation của `IPaymentPort` và phần wiring liên quan của Payment module.
  - Không ảnh hưởng logic module Order, Inventory, Reporting.

- **Đổi quy tắc seating** (ví dụ ràng buộc số khách/bàn):
  - Chỉ ảnh hưởng rule cụ thể trong luồng Order (validate lúc tạo/cập nhật order) và/hoặc Reporting nếu có chỉ số liên quan seating.
  - Không kéo theo thay đổi Payment gateway hay Inventory deduction.

- Tài liệu bounded contexts: `docs/bounded-contexts.md` (phân tách Order/Payment/Inventory/Reporting và reason-to-change).

## Runtime

- .NET: `net10.0`
- C#: `LangVersion=preview` (tương thích cú pháp hiện đại)

## DI và async

- Sử dụng `Microsoft.Extensions.DependencyInjection` tại `Program.cs`.
- Các port và use case dùng `Task` để hỗ trợ async workflow.

## Chạy thử

```bash
dotnet run
```
