## Kiểm tra kiến trúc / bounded context
- [ ] Module này có đúng **1 reason to change** không?
- [ ] Có method nào nằm ngoài bounded context của module không?
- [ ] Service Reporting chỉ đọc dữ liệu, không gọi mutation port.
- [ ] Service trong `Application.Order.*` không reference trực tiếp `IPaymentPort`.
