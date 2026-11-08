# Use case dự án FlashSale

| Thuộc tính | Nội dung |
|---|---|
| Phiên bản | 1.0 |
| Trạng thái | Bản nháp đã thống nhất phạm vi use case |
| Phạm vi | Chức năng khách hàng và quản lý cửa hàng |

## 1. Mục tiêu và phạm vi

Tài liệu mô tả mục đích và xử lý chính của các use case cho cửa hàng FlashSale. Đây là cơ sở để thiết kế API và bổ sung các tài liệu nghiệp vụ chi tiết hơn.

Các vai trò trong phạm vi gồm khách hàng, chủ cửa hàng, hệ thống xử lý đơn quá hạn và cổng thanh toán trực tuyến. Dashboard được để ngoài phạm vi hiện tại.

Các quy tắc nghiệp vụ và validation được quản lý riêng tại [BR-Project.md](BR-Project.md).

Đặc tả API input/output theo từng use case nằm tại [UseCase-Detail.md](UseCase-Detail.md).

## 2. Use case khách hàng

| Mã | Use case | Mục đích | Xử lý chính |
|---|---|---|---|
| CUS-01 | Xem danh mục | Giúp khách tìm sản phẩm theo nhóm hàng. | Trả danh sách category để hiển thị trên trang chủ. |
| CUS-02 | Xem FlashSale nổi bật | Giúp khách nhanh chóng thấy các ưu đãi lớn. | Trả tối đa 5 sản phẩm có FlashSale đang hiệu lực, xếp theo phần trăm giảm từ cao xuống thấp. |
| CUS-03 | Xem sản phẩm bán chạy | Giúp khách khám phá sản phẩm được mua nhiều. | Trả danh sách sản phẩm trên trang chủ, xếp theo số lượng đã bán giảm dần. |
| CUS-04 | Xem sản phẩm mới nhất | Giúp khách tìm sản phẩm vừa được thêm. | Trả danh sách sản phẩm mới, xếp theo thời điểm tạo gần nhất. |
| CUS-05 | Xem sản phẩm nổi bật | Giúp khách xem các sản phẩm được cửa hàng giới thiệu. | Trả sản phẩm đang được đánh dấu nổi bật. |
| CUS-06 | Duyệt sản phẩm FlashSale | Giúp khách so sánh giá và ưu đãi đang có. | Trả giá thường, giá FlashSale cuối cùng, category, tồn kho và phần trăm giảm; hỗ trợ sắp xếp phần trăm giảm tăng hoặc giảm. |
| CUS-07 | Duyệt sản phẩm | Giúp khách tìm và so sánh sản phẩm trong cửa hàng. | Trả danh sách sản phẩm, category và giá đang áp dụng; hỗ trợ xem theo mức bán chạy. |
| CUS-08 | Xem chi tiết sản phẩm | Giúp khách đánh giá sản phẩm trước khi mua. | Trả thông tin chi tiết, giá đang áp dụng và số lượng đã bán. |
| CUS-09 | Xem giỏ hàng | Giúp khách kiểm tra các sản phẩm đã chọn. | Trả sản phẩm trong giỏ, số lượng và giá đang áp dụng. |
| CUS-10 | Thêm sản phẩm vào giỏ | Giúp khách lưu sản phẩm muốn mua. | Thêm sản phẩm và số lượng khách chọn vào giỏ; kiểm tra sản phẩm và số lượng có thể mua. |
| CUS-11 | Sửa số lượng trong giỏ | Giúp khách điều chỉnh số hàng muốn mua. | Cập nhật số lượng của sản phẩm trong giỏ sau khi kiểm tra tồn kho khả dụng. |
| CUS-12 | Xóa sản phẩm khỏi giỏ | Giúp khách bỏ sản phẩm không còn muốn mua. | Xóa sản phẩm được chọn khỏi giỏ hàng. |
| CUS-13 | Xem lịch sử đơn hàng | Giúp khách theo dõi các lần mua và trạng thái xử lý. | Trả các đơn thuộc khách hàng đang đăng nhập, đơn mới nhất trước. |
| CUS-14 | Xem chi tiết đơn hàng | Giúp khách kiểm tra nội dung và giá của đơn đã đặt. | Trả sản phẩm, số lượng, giá đã chốt, thông tin thanh toán và trạng thái đơn. |
| CUS-15 | Tạo đơn hàng | Giúp khách đặt mua sản phẩm trong giỏ. | Tạo đơn từ giỏ hàng, lưu giá tại thời điểm đặt và giữ tồn kho. Nếu chọn online, bắt đầu hạn thanh toán một giờ. |
| CUS-16 | Thanh toán đơn hàng | Giúp khách thanh toán cho đơn đã tạo. | Tạo yêu cầu thanh toán riêng cho đơn và ghi nhận kết quả thanh toán từ cổng thanh toán. |

## 3. Use case tự động của hệ thống

| Mã | Use case | Mục đích | Xử lý chính |
|---|---|---|---|
| SYS-01 | Hủy đơn online quá hạn | Giải phóng đơn chưa được thanh toán trong thời hạn. | Khi đơn online vẫn chưa thanh toán sau một giờ, chuyển đơn sang trạng thái hủy và hoàn lại số lượng hàng đã giữ. Một đơn chỉ được hoàn kho một lần. |

## 4. Use case chủ cửa hàng

| Mã | Use case | Mục đích | Xử lý chính |
|---|---|---|---|
| STORE-01 | Danh sách category | Giúp cửa hàng tra cứu và quản lý danh mục. | Tìm kiếm và trả danh sách theo `pageIndex`, `pageSize`. |
| STORE-02 | Tạo category | Giúp cửa hàng bổ sung nhóm sản phẩm. | Tạo category từ thông tin được cung cấp. |
| STORE-03 | Cập nhật category | Giúp cửa hàng sửa thông tin danh mục. | Cập nhật category đã chọn. |
| STORE-04 | Cập nhật trạng thái category hàng loạt | Giúp cửa hàng nhanh chóng bật hoặc tắt nhiều danh mục. | Nhận danh sách ID và trạng thái đích, sau đó cập nhật các category được chọn. |
| STORE-05 | Danh sách sản phẩm | Giúp cửa hàng tra cứu và quản lý catalog. Lọc theo category, `sellingPrice` (giá FlashSale nếu đang áp dụng, nếu không thì giá thường), trạng thái; tìm kiếm và phân trang bằng `pageIndex`, `pageSize`. |
| STORE-06 | Tạo sản phẩm | Giúp cửa hàng đưa sản phẩm mới vào catalog. | Tạo sản phẩm với thông tin hàng hóa, giá thường, category và tồn kho. |
| STORE-07 | Cập nhật sản phẩm | Giúp cửa hàng sửa thông tin hoặc giá thường của sản phẩm. | Cập nhật sản phẩm đã chọn; giá FlashSale được quản lý trong use case FlashSale. |
| STORE-08 | Cập nhật trạng thái sản phẩm hàng loạt | Giúp cửa hàng bật hoặc tắt nhiều sản phẩm cùng lúc. | Nhận danh sách ID và trạng thái đích, sau đó cập nhật các sản phẩm được chọn. |
| STORE-09 | Danh sách FlashSale | Giúp cửa hàng tra cứu chương trình theo lịch và trạng thái. | Tìm kiếm, lọc theo ngày hoặc trạng thái và trả danh sách chương trình. |
| STORE-10 | Tạo FlashSale | Giúp cửa hàng lập chương trình cho các sản phẩm được chọn. | Khai báo sản phẩm, giá bán cuối cùng, số lượng áp dụng và thời gian chương trình. |
| STORE-11 | Cập nhật FlashSale | Giúp cửa hàng điều chỉnh chương trình đã tạo. | Sửa sản phẩm áp dụng, giá cuối cùng, số lượng hoặc thời gian chương trình. |
| STORE-12 | Cập nhật trạng thái FlashSale hàng loạt | Giúp cửa hàng bật hoặc tắt nhiều chương trình. | Nhận danh sách ID và trạng thái đích, sau đó cập nhật các FlashSale được chọn. |
| STORE-13 | Thiết lập giờ bắt đầu FlashSale | Giúp cửa hàng đổi thời điểm bắt đầu của chương trình. | Cập nhật giờ bắt đầu cho FlashSale đã chọn. |
| STORE-14 | Danh sách đơn hàng | Giúp cửa hàng tra cứu và sắp xếp đơn để xử lý. | Tìm theo khách hàng, lọc trạng thái, sắp xếp theo giá và phân trang. |
| STORE-15 | Xem chi tiết đơn hàng | Giúp cửa hàng kiểm tra nội dung và trạng thái của một đơn. | Trả thông tin khách hàng, sản phẩm, số lượng, giá đã chốt, thanh toán và trạng thái. |

## 5. Ngoài phạm vi hiện tại

- **Dashboard:** Chưa cần triển khai hoặc xác định các chỉ số trong giai đoạn này.

## 6. Luồng nghiệp vụ chính

Khách duyệt sản phẩm → xem chi tiết → thêm, sửa hoặc xóa sản phẩm trong giỏ → tạo đơn và chốt giá → thanh toán riêng. Nếu đơn online chưa thanh toán trong một giờ, hệ thống tự hủy đơn và hoàn lại số lượng hàng đã giữ.
