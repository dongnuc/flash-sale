# Business Rules — FlashSale

| Thuộc tính | Nội dung |
|---|---|
| Phiên bản | 1.0 |
| Trạng thái | Bản nháp để rà soát |
| Tài liệu use case | [UseCase-Project.md](UseCase-Project.md) |

Tài liệu này quản lý riêng business rule (BR) và validation rule (VAL). Tài liệu đặc tả API sẽ tham chiếu mã BR/VAL thay vì chép lại nội dung.

## 1. Business Rules

| Mã | Áp dụng | Quy tắc |
|---|---|---|
| BR-BIZ-001 | CUS-02, CUS-06–09, STORE-05, STORE-14 | Mọi response sản phẩm xác định giá đang bán từ FlashSale liên kết. FlashSale phải được bật, đang trong khoảng hiệu lực `[startsAt, endsAt)`, và còn quota để áp dụng giá sale. Nếu quota FlashSale đã hết nhưng sản phẩm còn tổng tồn kho, sản phẩm tiếp tục bán theo giá thường. |
| BR-BIZ-002 | CUS-02, CUS-06, STORE-10–11 | Chủ cửa hàng nhập giá bán cuối cùng `finalPrice`. Backend tính `discountPercent = (regularPrice - finalPrice) / regularPrice * 100`. Sắp xếp mức giảm theo phần trăm này. |
| BR-BIZ-003 | CUS-06, CUS-07, CUS-10–11, CUS-15, STORE-10–11, SYS-01 | FlashSale có quota riêng theo sản phẩm; quota ban đầu phải nhỏ hơn tổng `product.quantity`. Khi mua nhiều hơn quota còn lại, số lượng trong quota dùng `finalPrice`, phần vượt quota dùng giá thường nếu tổng tồn kho còn đủ. Nếu một sản phẩm có hai mức giá trong cùng đơn, lưu thành hai dòng đơn với giá snapshot riêng. Trừ tổng tồn kho theo toàn bộ số lượng; chỉ trừ quota FlashSale theo số lượng tính giá sale. Khi hủy đơn, hoàn lại đúng các lượng đã giữ ở từng kho. |
| BR-BIZ-004 | STORE-10–11, STORE-13 | Không cho hai FlashSale của cùng sản phẩm có khoảng thời gian áp dụng chồng lấn. Kiểm tra khi tạo, cập nhật thời gian hoặc đưa chương trình vào trạng thái có hiệu lực. Hai khoảng thời gian chồng lấn khi `startA < endB` và `startB < endA`. |
| BR-BIZ-005 | CUS-09–12 | Giá trong giỏ được tính lại theo giá hiện hành và chưa khóa. Thêm lại sản phẩm đã có thì cộng số lượng vào dòng hiện có. Sửa hoặc xóa chỉ tác động đến giỏ của khách đang đăng nhập; xóa giỏ không làm đổi tồn kho. |
| BR-BIZ-006 | CUS-15–16, CUS-14, STORE-15 | Khi tạo đơn, backend tự xác định giá theo BR-BIZ-001/003 và lưu giá từng dòng vào đơn. Thay đổi giá sản phẩm hoặc FlashSale sau đó không làm thay đổi đơn đã tạo. |
| BR-BIZ-007 | CUS-15 | Việc tạo đơn, lưu snapshot giá và giữ tồn kho phải nằm trong cùng transaction. Nếu bất kỳ dòng hàng nào không đủ tổng tồn kho, rollback toàn bộ đơn và toàn bộ thay đổi tồn kho. |
| BR-BIZ-008 | CUS-15–16, SYS-01 | Đơn chọn thanh toán online có hạn thanh toán một giờ tính từ `createdAt`. Đơn chưa thanh toán vẫn ở trạng thái chờ trong thời hạn này. |
| BR-BIZ-009 | SYS-01 | Khi đơn online chưa thanh toán đến hạn, hệ thống hủy đơn và hoàn lại đúng tổng tồn kho cùng quota FlashSale đã giữ. Cập nhật trạng thái và hoàn kho phải idempotent: một đơn chỉ được hoàn kho một lần. |
| BR-BIZ-010 | CUS-16, SYS-01 | Nếu cổng báo thanh toán thành công sau khi đơn đã bị hủy và hoàn kho, giữ nguyên đơn đã hủy, đánh dấu khoản thanh toán cần hoàn tiền thủ công và lưu vết giao dịch. Không tự khôi phục đơn hoặc tự động hoàn tiền. |
| BR-BIZ-011 | STORE-04, STORE-08, STORE-12 | Batch status là cập nhật trạng thái nhiều category, product hoặc FlashSale trong một transaction. Nếu một ID không hợp lệ, không tồn tại hoặc không thể cập nhật, rollback toàn bộ; không có kết quả cập nhật một phần. |
| BR-BIZ-012 | CUS-01 | Chỉ hiển thị category đang hoạt động. Không có category phù hợp trả danh sách rỗng thành công. |
| BR-BIZ-013 | CUS-02 | Trả tối đa 5 sản phẩm thuộc FlashSale đang hiệu lực và còn quota, sắp xếp theo `discountPercent` giảm dần. |
| BR-BIZ-014 | CUS-03 | Xếp sản phẩm theo số lượng trong các đơn đã thanh toán và chưa bị hủy; đơn hết hạn hoặc bị hủy không được tính vào lượng bán. |
| BR-BIZ-015 | CUS-04 | Xếp sản phẩm theo `createdAt` giảm dần. |
| BR-BIZ-016 | CUS-05 | Chỉ trả sản phẩm đang hoạt động và được đánh dấu nổi bật. |
| BR-BIZ-017 | CUS-06 | Chỉ trả sản phẩm FlashSale đang hiệu lực và còn quota; response có giá thường, giá cuối, phần trăm giảm, category và quota còn lại. Hỗ trợ sắp xếp phần trăm giảm tăng hoặc giảm. |
| BR-BIZ-018 | CUS-07–08 | Chỉ trả sản phẩm được phép hiển thị; mọi giá dùng BR-BIZ-001/003. Chi tiết sản phẩm gồm số lượng đã bán. |
| BR-BIZ-019 | CUS-09–12 | Khách chỉ được xem và sửa giỏ hàng của chính mình. Kiểm tra khả năng bán tại lúc thêm/sửa; việc kiểm tra lại cuối cùng diễn ra khi tạo đơn. |
| BR-BIZ-020 | CUS-13–14 | Khách chỉ được xem đơn của mình. Lịch sử mặc định đơn mới nhất trước; chi tiết đơn trả giá snapshot, không thay bằng giá hiện tại. |
| BR-BIZ-021 | STORE-01–04 | Chủ cửa hàng được xem category theo trạng thái, tạo và cập nhật category. Tên category phải tuân thủ VAL-001. Batch status tuân thủ BR-BIZ-011. |
| BR-BIZ-022 | STORE-05–08 | Chủ cửa hàng được lọc product theo các tham số được gửi và quản lý product. `minPrice`/`maxPrice` của STORE-05 lọc theo `sellingPrice`: dùng giá FlashSale khi chương trình hiệu lực và còn quota, ngược lại dùng giá thường. Tên tuân thủ VAL-001; giá và tồn kho tuân thủ VAL-002/004. Không giảm tồn kho xuống thấp hơn lượng đang được đơn chờ xử lý giữ. Batch status tuân thủ BR-BIZ-011. |
| BR-BIZ-023 | STORE-09–13 | Chủ cửa hàng được tìm và lọc FlashSale theo ngày, từ khóa và trạng thái; quản lý giá cuối và quota theo BR-BIZ-002/003; không tạo lịch chồng lấn theo BR-BIZ-004. Batch status tuân thủ BR-BIZ-011. |
| BR-BIZ-024 | STORE-14–15 | Chủ cửa hàng được tra cứu mọi đơn theo khách hàng/trạng thái và sắp xếp theo tổng tiền. Chi tiết đơn trả thông tin khách, hàng hóa, snapshot giá và thanh toán. |

## 2. Validation Rules

Validation dùng chung được định nghĩa một lần; use case tham chiếu các mã VAL liên quan.

| Mã | Quy tắc validation | Áp dụng cho |
|---|---|---|
| VAL-001 | So trùng tên trong cùng loại đối tượng sau khi chuyển tên về chữ hoa. Khi cập nhật, loại ID hiện tại khỏi phép so sánh. | `category.name` ở STORE-02/03; `product.name` ở STORE-06/07. Không so tên category với tên product. |
| VAL-002 | Giá phải là số tiền lớn hơn `0`. | `regularPrice` ở STORE-06/07; `finalPrice` ở STORE-10/11. |
| VAL-003 | Số lượng phải là số nguyên lớn hơn `0`. | `quantity` ở CUS-10/11; số lượng FlashSale ở STORE-10/11. |
| VAL-004 | Tổng tồn kho product phải là số nguyên không âm; `0` hợp lệ. | `product.quantity` ở STORE-06/07. |
| VAL-005 | Quota FlashSale ban đầu phải nhỏ hơn tổng `product.quantity`. | STORE-10/11. Đây là phép so sánh nghiêm ngặt `<`, theo yêu cầu đã xác nhận. |
| VAL-006 | ID phải đúng định dạng `Guid`. ID hợp lệ về định dạng nhưng không tồn tại được xử lý là đối tượng không tìm thấy. | Tất cả URL và body có ID. |
| VAL-007 | `pageIndex` mặc định `1`, `pageSize` mặc định `10`; `pageIndex >= 1`, `1 <= pageSize <= 100`. | CUS-03–07, CUS-13, STORE-01, STORE-05, STORE-09, STORE-14. |
| VAL-008 | Thời gian nhận vào phải đúng ISO 8601 UTC; `endsAt` phải sau `startsAt`. | STORE-10/11/13. |
| VAL-009 | Các enum như trạng thái, sắp xếp, collection và phương thức thanh toán chỉ nhận giá trị API đã khai báo. | Các use case nhận giá trị enum. Danh sách enum cụ thể được đặc tả trong API contract. |
| VAL-010 | Danh sách batch phải có ít nhất một ID, không có ID trùng; từng ID phải tồn tại và đạt điều kiện cập nhật. | STORE-04/08/12; nếu một mục lỗi thì áp dụng rollback theo BR-BIZ-011. |
| VAL-011 | Nếu có cả `minPrice` và `maxPrice`, phải thỏa `minPrice <= maxPrice`. | STORE-05. |
| VAL-012 | Các trường bắt buộc không được thiếu hoặc rỗng: category cần `name`; product cần `name`, `categoryId`, `regularPrice`, `quantity`; FlashSale cần sản phẩm, `startsAt`, `endsAt`; dòng giỏ cần `productId`, `quantity`; checkout cần phương thức thanh toán và thông tin giao hàng. | STORE-02/03, STORE-06/07, STORE-10/11, CUS-10/11, CUS-15. Lỗi dữ liệu trả validation error; tham chiếu không tồn tại trả not found; xung đột tồn kho/lịch trả conflict. |

### Trạng thái và giới hạn tài liệu

- **Dashboard** đang ngoài phạm vi, nên chưa có BR.
- Tên trạng thái cụ thể và các phương thức thanh toán ngoài online sẽ được chốt trong đặc tả API.
