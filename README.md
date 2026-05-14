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

- `src/Domain`
 - Entities/value objects: `Order`, `MenuItem`, `DiningTable`, `InventoryItem`.
 - Business rules: trạng thái order (`Draft -> SentToKitchen -> Paid -> Closed`), add/remove món, validate flow.
- `src/Application`
 - Use case: `RunHudRoFnbUseCase`.
 - Command models cho từng thao tác nghiệp vụ.
 - Ports: `IFnbReadPort`, `IOrderPort`, `IInventoryPort`, `IPaymentPort`.
- `src/Infrastructure`
 - In-memory adapters cho đọc dữ liệu, order persistence, inventory, payment gateway.
- `src/Adapters/Console`
 - Presenter hiển thị bill và báo cáo cuối ca.

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
