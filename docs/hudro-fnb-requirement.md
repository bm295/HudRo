# HudRo FnB Management Requirement

## Input requirement

- Nhà hàng: **HudRo**
- Quy mô: khoảng **40–60 chỗ**
- Mục tiêu: xây dựng ứng dụng quản lý FnB.

## Scope implemented in this repository

1. Quản lý profile nhà hàng và cấu hình bàn trong dải 40–60 ghế.
2. Quản lý menu và inventory theo SKU.
3. Quản lý order theo vòng đời:
  - Create order cho bàn.
  - Add / remove items.
  - Send to kitchen.
  - Process payment.
  - Deduct inventory.
  - Close order.
4. Báo cáo cuối ca:
  - Tổng số khách đã phục vụ.
  - Số order đã đóng.
  - Tổng doanh thu.

## Constraints

- Tổng số ghế cấu hình phải nằm trong dải **40–60**; nếu vượt phạm vi sẽ báo lỗi nghiệp vụ.
- Không thể thanh toán khi order chưa gửi bếp.
- Không thể đóng order khi chưa thanh toán.
