# Bounded Contexts cho HudRo FnB

Tài liệu này tách rõ phạm vi nghiệp vụ để tránh chồng chéo logic giữa các module trong hệ thống quản lý nhà hàng.

## 1) Order Context

**Mục tiêu:** quản lý toàn bộ vòng đời order bàn ăn từ lúc mở bàn đến lúc đóng order.

### Inputs/Outputs chính

- **Inputs**
  - Yêu cầu mở order theo bàn.
  - Yêu cầu thêm/xoá món khỏi order.
  - Yêu cầu gửi bếp.
  - Tín hiệu xác nhận thanh toán thành công (từ Payment Context).
- **Outputs**
  - Trạng thái order hiện tại (`Draft`, `SentToKitchen`, `Paid`, `Closed`).
  - Danh sách line items cuối cùng của order.
  - Sự kiện/ngữ nghĩa nghiệp vụ: order đã gửi bếp, order đã đóng.

### Business rules thuộc scope

- Chỉ cho phép thao tác add/remove món khi order còn ở trạng thái phù hợp.
- Luồng trạng thái bắt buộc: `Draft -> SentToKitchen -> Paid -> Closed`.
- Không thể đóng order nếu chưa thanh toán thành công.
- Thông tin bàn và order phải nhất quán trong suốt vòng đời order.

### Anti-scope (không thuộc scope)

- Không xử lý kết nối cổng thanh toán hoặc sinh payment reference.
- Không thực hiện logic giữ/trừ tồn kho chi tiết.
- Không tổng hợp báo cáo cuối ca đa order.

### Reason to change (Order Module)

| Module | Reason to change |
|---|---|
| `Order` domain model / `IOrderPort` | Khi thay đổi quy tắc vòng đời order, trạng thái hợp lệ hoặc hành vi add/remove món. |

---

## 2) Payment Context

**Mục tiêu:** xử lý thanh toán và quản lý payment reference cho từng order.

### Inputs/Outputs chính

- **Inputs**
  - Yêu cầu thanh toán cho một order với tổng tiền cần thu.
  - Thông tin phương thức thanh toán (tiền mặt/thẻ/ví...).
- **Outputs**
  - Kết quả thanh toán (thành công/thất bại).
  - `payment reference` duy nhất để đối soát.
  - Tín hiệu thanh toán thành công để Order Context chuyển trạng thái sang `Paid`.

### Business rules thuộc scope

- Mỗi giao dịch thanh toán phải có payment reference duy nhất.
- Không cho phép xác nhận một order là đã thanh toán nếu gateway trả thất bại.
- Lưu/tra cứu trạng thái giao dịch phục vụ đối soát cơ bản.

### Anti-scope (không thuộc scope)

- Không quyết định trạng thái vòng đời order ngoài tín hiệu thanh toán.
- Không kiểm tra/điều phối tồn kho.
- Không chịu trách nhiệm tổng hợp số liệu cuối ca theo toàn hệ thống.

### Reason to change (Payment Module)

| Module | Reason to change |
|---|---|
| `IPaymentPort` + payment adapter | Khi thay đổi cổng thanh toán, cơ chế payment reference, hoặc chính sách đối soát. |

---

## 3) Inventory Context

**Mục tiêu:** quản lý tồn kho, bao gồm giữ kho (reserve) và trừ kho (deduct) theo nhu cầu vận hành.

### Inputs/Outputs chính

- **Inputs**
  - Danh sách món/số lượng cần giữ kho khi order được chấp nhận.
  - Xác nhận trừ kho khi nghiệp vụ đạt mốc phù hợp (ví dụ sau thanh toán hoặc theo policy).
  - Yêu cầu hoàn giữ kho khi order bị huỷ trước ngưỡng cam kết.
- **Outputs**
  - Trạng thái giữ kho thành công/thất bại.
  - Số lượng tồn kho khả dụng sau mỗi nghiệp vụ.
  - Bút toán/ghi nhận biến động kho để phục vụ audit nội bộ.

### Business rules thuộc scope

- Không cho phép giữ/trừ vượt quá tồn kho khả dụng.
- Phân biệt rõ lượng **đã giữ** và lượng **đã trừ thực tế**.
- Bảo toàn nhất quán tồn kho khi có thao tác rollback phù hợp policy.

### Anti-scope (không thuộc scope)

- Không quản lý trạng thái nghiệp vụ order (Draft/Paid/Closed).
- Không xử lý payment reference hoặc giao dịch thanh toán.
- Không chịu trách nhiệm trình bày báo cáo tài chính cuối ca.

### Reason to change (Inventory Module)

| Module | Reason to change |
|---|---|
| `IInventoryPort` + inventory adapter/store | Khi thay đổi chính sách reserve/deduct, quy tắc khả dụng tồn kho, hoặc mô hình audit kho. |

---

## 4) Reporting Context

**Mục tiêu:** tổng hợp số liệu cuối ca từ các context nghiệp vụ để phục vụ vận hành.

### Inputs/Outputs chính

- **Inputs**
  - Dữ liệu order đã đóng trong ca.
  - Dữ liệu giao dịch thanh toán và payment reference.
  - Dữ liệu biến động kho liên quan ca làm việc.
- **Outputs**
  - Báo cáo cuối ca: doanh thu, số order, tỷ lệ thanh toán thành công.
  - Báo cáo kiểm soát: chênh lệch tồn kho, giao dịch cần đối soát.

### Business rules thuộc scope

- Chỉ tổng hợp dữ liệu đã chốt theo mốc thời gian ca.
- Chuẩn hoá chỉ số báo cáo và công thức tính nhất quán giữa các ca.
- Có khả năng trace ngược tới order/payment reference khi cần kiểm tra.

### Anti-scope (không thuộc scope)

- Không sửa trực tiếp trạng thái order.
- Không phát sinh giao dịch thanh toán mới.
- Không thực hiện ghi giảm tồn kho nghiệp vụ.

### Reason to change (Reporting Module)

| Module | Reason to change |
|---|---|
| Read model / presenter báo cáo cuối ca | Khi thay đổi KPI vận hành, định nghĩa chỉ số báo cáo, hoặc format đầu ra cho quản lý. |

---

## Bảng reason to change tổng hợp (tránh chồng chéo)

| Context | Thành phần chính | Lý do thay đổi hợp lệ |
|---|---|---|
| Order | Domain order + `IOrderPort` | Thay đổi vòng đời order, rule add/remove món, rule đóng order. |
| Payment | `IPaymentPort` + payment adapter | Thay đổi cổng thanh toán, mã tham chiếu, quy tắc đối soát giao dịch. |
| Inventory | `IInventoryPort` + inventory store/adapter | Thay đổi chính sách giữ/trừ kho, kiểm soát khả dụng, quy tắc rollback kho. |
| Reporting | Read/report presenter | Thay đổi KPI cuối ca, công thức tổng hợp, định dạng báo cáo. |

