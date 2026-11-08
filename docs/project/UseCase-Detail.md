# Đặc tả API theo use case — FlashSale

| Thuộc tính | Nội dung |
|---|---|
| Phiên bản | 0.1 |
| Trạng thái | Bản nháp để rà soát |
| Use case nguồn | [UseCase-Project.md](UseCase-Project.md) |
| BR/Validation | [BR-Project.md](BR-Project.md) |

Tài liệu đặc tả input và output cho từng use case. Các lỗi nghiệp vụ dự kiến được Application trả bằng `Result` thất bại; API Presentation ánh xạ mã lỗi sang HTTP status. Chỉ một số use case đại diện có ví dụ lỗi để tránh lặp.

## 1. Quy ước API và response

- Base URL: `/api/v1`. Route quản trị nằm dưới `/admin`; route dữ liệu của khách hàng hiện đăng nhập nằm dưới `/me`.
- ID dùng `Guid`; thời gian dùng ISO 8601 UTC; giá tiền dùng số VND.
- Pagination: `pageIndex=1`, `pageSize=10`; tối đa `pageSize=100`.
- Response thành công của `Result<T>` dùng camelCase theo JSON mặc định của ASP.NET Core:

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": "",
    "message": ""
  },
  "value": {}
}
```

- Với lệnh không trả dữ liệu, Application dùng `Result.Success()`; response không có `value`.
- Application trả lỗi bằng `Result.Failure<T>(new Error(code, message))`. Trong codebase hiện tại factory tên là `Failure`, chưa có method `Fail`. Không serialize trực tiếp `Result<T>` thất bại vì getter `Value` sẽ ném exception; endpoint phải chuyển lỗi thành HTTP response.
- HTTP response được mô tả bằng status code bên ngoài (`200`, `400`, `404`, `409`, `500`). Body lỗi dùng `errors` gồm `header` là mã/tên lỗi và `description` là message.
- Trong các ví dụ lỗi, `applicationResult` chỉ mô tả kết quả nội bộ của use case; nó không nằm trong HTTP response body. Body thực tế được tạo ở Presentation từ `Error.Code` và `Error.Message`.
- Các route `/me` yêu cầu khách đăng nhập. Các route `/admin` yêu cầu quyền chủ cửa hàng; `userId` được lấy từ claims, không nhận từ request body.

### 1.1 Kiểu dữ liệu output dùng chung

#### CategoryDto

```json
{
  "id": "guid",
  "name": "string",
  "slug": "string"
}
```

#### AdminCategoryDto

```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "description": "string",
  "status": "string"
}
```

#### ProductCardDto

```json
{
  "id": "guid",
  "name": "string",
  "thumbnailUrl": "string|null",
  "category": {
    "id": "guid",
    "name": "string"
  },
  "regularPrice": 100000,
  "sellingPrice": 80000,
  "isFlashSale": true,
  "flashSaleId": "guid|null",
  "discountPercent": 20,
  "productQuantity": 50,
  "flashSaleQuantityRemaining": 10,
  "soldQuantity": 120
}
```

Khi quota FlashSale hết mà tồn kho tổng còn, `sellingPrice` bằng `regularPrice`, `isFlashSale=false`, `discountPercent=0`, `flashSaleQuantityRemaining=0`.

#### ProductDetailDto

```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "images": ["string"],
  "attributes": [
    {
      "name": "string",
      "value": "string"
    }
  ],
  "category": {
    "id": "guid",
    "name": "string"
  },
  "regularPrice": 100000,
  "sellingPrice": 80000,
  "isFlashSale": true,
  "flashSaleId": "guid|null",
  "discountPercent": 20,
  "productQuantity": 50,
  "flashSaleQuantityRemaining": 10,
  "soldQuantity": 120
}
```

`images` và `attributes` là cấu trúc dự kiến; chốt lại theo model sản phẩm thực tế.

#### AdminProductDto

```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "categoryId": "guid",
  "categoryName": "string",
  "regularPrice": 100000,
  "sellingPrice": 80000,
  "discountPercent": 20,
  "quantity": 50,
  "isFeatured": false,
  "status": "string",
  "createdAt": "2026-11-06T10:00:00Z",
  "updatedAt": "2026-11-06T10:00:00Z"
}
```

#### Page<T>

```json
{
  "items": ["T"],
  "pageIndex": 1,
  "pageSize": 10,
  "totalCount": 25,
  "hasNextPage": true,
  "hasPrevious": false
}
```

#### CartDto

```json
{
  "cartId": "guid",
  "items": [
    {
      "productId": "guid",
      "productName": "string",
      "quantity": 3,
      "priceBreakdown": [
        {
          "priceType": "FLASH_SALE",
          "quantity": 2,
          "unitPrice": 80000,
          "lineTotal": 160000
        },
        {
          "priceType": "REGULAR",
          "quantity": 1,
          "unitPrice": 100000,
          "lineTotal": 100000
        }
      ],
      "estimatedLineTotal": 260000
    }
  ],
  "totalQuantity": 3,
  "estimatedTotal": 260000
}
```

Giá trong giỏ chỉ là giá ước tính hiện tại; giá được chốt lúc tạo đơn.

#### OrderSummaryDto và OrderDetailDto

```json
{
  "orderId": "guid",
  "orderCode": "FS-20261106-0001",
  "status": "string",
  "paymentMethod": "string",
  "paymentStatus": "string",
  "totalAmount": 260000,
  "createdAt": "2026-11-06T10:00:00Z",
  "paymentDeadline": "2026-11-06T11:00:00Z|null"
}
```

`OrderDetailDto` mở rộng summary và trả đầy đủ các trường sau:

```json
{
  "orderId": "guid",
  "orderCode": "FS-20261106-0001",
  "status": "string",
  "paymentMethod": "string",
  "paymentStatus": "string",
  "items": [
    {
      "productId": "guid",
      "productName": "string",
      "priceType": "FLASH_SALE|REGULAR",
      "flashSaleId": "guid|null",
      "quantity": 2,
      "unitPrice": 80000,
      "lineTotal": 160000
    }
  ],
  "subtotal": 160000,
  "shippingFee": 0,
  "totalAmount": 160000,
  "deliveryAddress": {
    "recipientName": "string",
    "phone": "string",
    "province": "string",
    "district": "string",
    "ward": "string",
    "addressLine": "string"
  },
  "createdAt": "2026-11-06T10:00:00Z",
  "paymentDeadline": "2026-11-06T11:00:00Z|null"
}
```

#### FlashSaleDto, PaymentIntentDto, BatchStatusDto

```json
{
  "id": "guid",
  "name": "string",
  "startsAt": "2026-11-07T02:00:00Z",
  "endsAt": "2026-11-08T02:00:00Z",
  "status": "string",
  "items": [
    {
      "productId": "guid",
      "productName": "string",
      "regularPrice": 100000,
      "finalPrice": 80000,
      "discountPercent": 20,
      "flashSaleQuantity": 20,
      "remainingQuantity": 20
    }
  ]
}
```

```json
{
  "paymentId": "guid",
  "provider": "string",
  "checkoutUrl": "https://payment-provider/checkout/...",
  "amount": 260000,
  "currency": "VND",
  "expiresAt": "2026-11-06T11:00:00Z"
}
```

```json
{
  "requestedCount": 2,
  "updatedCount": 2,
  "updatedIds": ["guid-1", "guid-2"],
  "status": "INACTIVE"
}
```

## 2. Use case khách hàng

### CUS-01 — Xem danh mục

- **API:** `GET /api/v1/categories`
- **Input:** Không có path params, query params hoặc body.
- **Output:** HTTP `200`, `Result.Success<CategoryDto[]>`; chỉ trả category đang hoạt động. Danh sách rỗng trả `value: []`.
- **BR/VAL:** `BR-BIZ-012`.

### CUS-02 — Xem FlashSale nổi bật

- **API:** `GET /api/v1/home/flash-sales`
- **Input:** Không nhận limit từ client; backend cố định tối đa 5.
- **Output:** HTTP `200`, `Result.Success<ProductCardDto[]>`; lấy tối đa 5 sản phẩm đang FlashSale và còn quota, xếp `discountPercent` giảm dần.
- **BR/VAL:** `BR-BIZ-001`–`003`, `BR-BIZ-013`.

### CUS-03 — Xem sản phẩm bán chạy

- **API:** `GET /api/v1/home/products/best-selling`
- **Input:** Query `pageIndex` (mặc định 1), `pageSize` (mặc định 10, tối đa 100).
- **Output:** HTTP `200`, `Result.Success<Page<ProductCardDto>>`; xếp lượng bán giảm dần.
- **BR/VAL:** `BR-BIZ-014`, `VAL-007`.

### CUS-04 — Xem sản phẩm mới nhất

- **API:** `GET /api/v1/home/products/newest`
- **Input:** Query `pageIndex`, `pageSize` theo `VAL-007`.
- **Output:** HTTP `200`, `Result.Success<Page<ProductCardDto>>`; sản phẩm có `createdAt` mới nhất đứng trước.
- **BR/VAL:** `BR-BIZ-015`, `VAL-007`.

### CUS-05 — Xem sản phẩm nổi bật

- **API:** `GET /api/v1/home/products/featured`
- **Input:** Query `pageIndex`, `pageSize` theo `VAL-007`.
- **Output:** HTTP `200`, `Result.Success<Page<ProductCardDto>>`; chỉ gồm sản phẩm hoạt động và được đánh dấu nổi bật.
- **BR/VAL:** `BR-BIZ-016`, `VAL-007`.

### CUS-06 — Duyệt sản phẩm FlashSale

- **API:** `GET /api/v1/flash-sales/products`
- **Input:** Query tùy chọn `categoryId: Guid`; `sortByDiscount: ASC|DESC` (mặc định `DESC`); `pageIndex`, `pageSize` theo `VAL-007`.
- **Output:** HTTP `200`, `Result.Success<Page<ProductCardDto>>`; gồm giá thường, giá đang bán, phần trăm giảm, category, tổng tồn kho và quota FlashSale còn lại.
- **BR/VAL:** `BR-BIZ-001`–`003`, `BR-BIZ-017`, `VAL-006`–`007`, `VAL-009`.

### CUS-07 — Duyệt sản phẩm

- **API:** `GET /api/v1/products`
- **Input:** Query tùy chọn `categoryId: Guid`; `sortBy=bestSelling` (mặc định); `pageIndex`, `pageSize` theo `VAL-007`.
- **Output:** HTTP `200`, `Result.Success<Page<ProductCardDto>>`; giá dùng quy tắc hiện hành; quota sale hết thì giá trả về là giá thường.
- **BR/VAL:** `BR-BIZ-001`, `BR-BIZ-003`, `BR-BIZ-018`, `VAL-006`–`007`, `VAL-009`.

### CUS-08 — Xem chi tiết sản phẩm

- **API:** `GET /api/v1/products/{productId}`
- **Input:** Path `productId: Guid`.
- **Output:** HTTP `200`, `Result.Success<ProductDetailDto>`.
- **BR/VAL:** `BR-BIZ-001`, `BR-BIZ-018`, `VAL-006`.

### CUS-09 — Xem giỏ hàng

- **API:** `GET /api/v1/me/cart`
- **Input:** Không có params/body; `userId` lấy từ claims.
- **Output:** HTTP `200`, `Result.Success<CartDto>`; giá và tổng tiền là ước tính tại thời điểm truy vấn.
- **BR/VAL:** `BR-BIZ-005`, `BR-BIZ-019`.

### CUS-10 — Thêm sản phẩm vào giỏ

- **API:** `POST /api/v1/me/cart/items`
- **Input body:**

```json
{
  "productId": "product-guid",
  "quantity": 2
}
```

- **Output:** HTTP `200`, `Result.Success<CartDto>` sau cập nhật. Nếu sản phẩm đã có, cộng số lượng vào dòng hiện tại.
- **BR/VAL:** `BR-BIZ-005`, `BR-BIZ-019`, `VAL-003`, `VAL-006`.

### CUS-11 — Sửa số lượng trong giỏ

- **API:** `PATCH /api/v1/me/cart/items/{productId}`
- **Input:** Path `productId: Guid`; body:

```json
{
  "quantity": 3
}
```

- **Output:** HTTP `200`, `Result.Success<CartDto>` sau cập nhật. Kiểm tra lượng có thể bán; muốn xóa dòng hàng thì dùng CUS-12.
- **BR/VAL:** `BR-BIZ-005`, `BR-BIZ-019`, `VAL-003`, `VAL-006`.

### CUS-12 — Xóa sản phẩm khỏi giỏ

- **API:** `DELETE /api/v1/me/cart/items/{productId}`
- **Input:** Path `productId: Guid`; không có body.
- **Output:** HTTP `200`, `Result.Success<CartDto>` sau khi xóa.
- **BR/VAL:** `BR-BIZ-005`, `BR-BIZ-019`, `VAL-006`.

### CUS-13 — Xem lịch sử đơn hàng

- **API:** `GET /api/v1/me/orders`
- **Input:** Query tùy chọn `status`; `pageIndex`, `pageSize` theo `VAL-007`; chỉ truy vấn đơn của khách đang đăng nhập.
- **Output:** HTTP `200`, `Result.Success<Page<OrderSummaryDto>>`; đơn mới nhất đứng trước.
- **BR/VAL:** `BR-BIZ-020`, `VAL-007`, `VAL-009`.

### CUS-14 — Xem chi tiết đơn hàng

- **API:** `GET /api/v1/me/orders/{orderId}`
- **Input:** Path `orderId: Guid`; chỉ cho xem đơn của khách đang đăng nhập.
- **Output:** HTTP `200`, `Result.Success<OrderDetailDto>` với giá snapshot đã chốt.
- **BR/VAL:** `BR-BIZ-006`, `BR-BIZ-020`, `VAL-006`.

### CUS-15 — Tạo đơn hàng

- **API:** `POST /api/v1/me/orders`
- **Input body:** Backend lấy items từ giỏ; client không gửi giá hoặc tổng tiền.

```json
{
  "paymentMethod": "ONLINE",
  "deliveryAddress": {
    "recipientName": "Nguyen Van A",
    "phone": "0900000000",
    "province": "Tinh A",
    "district": "Quan B",
    "ward": "Phuong C",
    "addressLine": "So 1, duong X"
  },
  "note": "Giao giờ hành chính"
}
```

- **Output:** HTTP `201`, `Result.Success<OrderDetailDto>`. Giá được tính lại tại checkout, lưu snapshot; phần quota FlashSale dùng giá sale, phần vượt quota dùng giá thường. Đơn online có `paymentDeadline = createdAt + 1 giờ`.
- **BR/VAL:** `BR-BIZ-003`, `BR-BIZ-006`–`008`, `VAL-009`, `VAL-012`.

### CUS-16 — Thanh toán đơn hàng

- **API:** `POST /api/v1/me/orders/{orderId}/payment`
- **Input:** Path `orderId: Guid`; không nhận số tiền từ client.
- **Output:** HTTP `201`, `Result.Success<PaymentIntentDto>`; chỉ tạo cho đơn của khách đó đang chờ thanh toán và chưa quá hạn.
- **BR/VAL:** `BR-BIZ-008`, `BR-BIZ-010`, `VAL-006`.

#### API tích hợp cổng thanh toán thuộc CUS-16

- **API:** `POST /api/v1/payments/webhooks/{provider}`
- **Input:** Path `provider`; chữ ký provider trong header; body theo payload đã ký của provider. Không tin kết quả thanh toán do client gửi.
- **Output:** HTTP `200`, body xác nhận đã nhận callback. Nếu callback báo thành công muộn sau khi đơn bị hủy, đánh dấu khoản thanh toán cần hoàn tiền thủ công; không khôi phục đơn.
- **BR/VAL:** `BR-BIZ-010`.

## 3. Use case tự động của hệ thống

### SYS-01 — Hủy đơn online quá hạn

- **Trigger:** Background worker; không có URL công khai.
- **Input nội bộ:** `utcNow`, `batchSize` từ cấu hình.
- **Điều kiện xử lý:** Đơn online đang chờ thanh toán và `paymentDeadline <= utcNow`.
- **Output/log:** `scannedCount`, `cancelledCount`, `restockedCount`, `failedCount`, `runAt`.
- **Xử lý lỗi:** Lỗi nghiệp vụ trên một đơn không được làm hoàn kho lặp; lỗi kỹ thuật cần log và retry theo chính sách worker.
- **BR/VAL:** `BR-BIZ-003`, `BR-BIZ-009`–`010`.

## 4. Use case chủ cửa hàng

Tất cả route `/admin` yêu cầu xác thực và quyền chủ cửa hàng. Trạng thái cụ thể dùng enum được cấu hình trong API; ví dụ bên dưới dùng `ACTIVE` và `INACTIVE`.

### STORE-01 — Danh sách category

- **API:** `GET /api/v1/admin/categories`
- **Input query:** `search?: string`, `pageIndex?: int=1`, `pageSize?: int=10`.
- **Output:** HTTP `200`, `Result.Success<Page<AdminCategoryDto>>`; có thể trả cả category đang bật và tắt.
- **BR/VAL:** `BR-BIZ-021`, `VAL-007`.

### STORE-02 — Tạo category

- **API:** `POST /api/v1/admin/categories`
- **Input body:**

```json
{
  "name": "Điện tử",
  "description": "Thiết bị điện tử"
}
```

- **Output:** HTTP `201`, `Result.Success<AdminCategoryDto>`.
- **BR/VAL:** `BR-BIZ-021`, `VAL-001`, `VAL-012`.

### STORE-03 — Cập nhật category

- **API:** `PUT /api/v1/admin/categories/{categoryId}`
- **Input:** Path `categoryId: Guid`; body gồm `name: string`, `description: string`.
- **Output:** HTTP `200`, `Result.Success<AdminCategoryDto>` sau cập nhật.
- **BR/VAL:** `BR-BIZ-021`, `VAL-001`, `VAL-006`, `VAL-012`.

### STORE-04 — Cập nhật trạng thái category hàng loạt

- **API:** `PATCH /api/v1/admin/categories/batch-status`
- **Input body:**

```json
{
  "ids": ["category-guid-1", "category-guid-2"],
  "status": "INACTIVE"
}
```

- **Output:** HTTP `200`, `Result.Success<BatchStatusDto>`. Toàn bộ cập nhật trong một transaction; một ID lỗi thì rollback toàn bộ.
- **BR/VAL:** `BR-BIZ-011`, `BR-BIZ-021`, `VAL-006`, `VAL-009`–`010`.

### STORE-05 — Danh sách sản phẩm

- **API:** `GET /api/v1/admin/products`
- **Input query:** `categoryId?: Guid`, `minPrice?: decimal`, `maxPrice?: decimal`, `search?: string`, `status?: string`, `pageIndex?: int=1`, `pageSize?: int=10`.
- **Quy tắc lọc giá:** `minPrice` và `maxPrice` áp dụng trên `sellingPrice`; nếu FlashSale đang hiệu lực và còn quota thì dùng giá FlashSale, nếu không thì dùng giá thường.
- **Output:** HTTP `200`, `Result.Success<Page<AdminProductDto>>`.
- **BR/VAL:** `BR-BIZ-022`, `VAL-006`–`007`, `VAL-009`, `VAL-011`.

### STORE-06 — Tạo sản phẩm

- **API:** `POST /api/v1/admin/products`
- **Input body:**

```json
{
  "name": "Áo thun",
  "description": "Áo cotton",
  "categoryId": "category-guid",
  "regularPrice": 100000,
  "quantity": 50,
  "isFeatured": false,
  "status": "ACTIVE"
}
```

- **Output:** HTTP `201`, `Result.Success<AdminProductDto>`.
- **BR/VAL:** `BR-BIZ-022`, `VAL-001`–`002`, `VAL-004`, `VAL-006`, `VAL-012`.

### STORE-07 — Cập nhật sản phẩm

- **API:** `PUT /api/v1/admin/products/{productId}`
- **Input:** Path `productId: Guid`; body có cùng trường với STORE-06.
- **Output:** HTTP `200`, `Result.Success<AdminProductDto>` sau cập nhật. Không cho giảm tồn kho thấp hơn lượng đang được đơn chờ xử lý giữ.
- **BR/VAL:** `BR-BIZ-006`, `BR-BIZ-022`, `VAL-001`–`002`, `VAL-004`, `VAL-006`, `VAL-012`.

### STORE-08 — Cập nhật trạng thái sản phẩm hàng loạt

- **API:** `PATCH /api/v1/admin/products/batch-status`
- **Input body:** `ids: Guid[]`, `status: string`.

```json
{
  "ids": ["product-guid-1", "product-guid-2"],
  "status": "INACTIVE"
}
```

- **Output:** HTTP `200`, `Result.Success<BatchStatusDto>`. Một ID lỗi thì rollback toàn bộ batch.
- **BR/VAL:** `BR-BIZ-011`, `BR-BIZ-022`, `VAL-006`, `VAL-009`–`010`.

### STORE-09 — Danh sách FlashSale

- **API:** `GET /api/v1/admin/flash-sales`
- **Input query:** `dateFrom?: datetime`, `dateTo?: datetime`, `search?: string`, `status?: string`, `pageIndex?: int=1`, `pageSize?: int=10`.
- **Output:** HTTP `200`, `Result.Success<Page<FlashSaleDto>>`.
- **BR/VAL:** `BR-BIZ-023`, `VAL-007`–`009`.

### STORE-10 — Tạo FlashSale

- **API:** `POST /api/v1/admin/flash-sales`
- **Input body:**

```json
{
  "name": "Flash Sale cuối tuần",
  "startsAt": "2026-11-07T02:00:00Z",
  "endsAt": "2026-11-08T02:00:00Z",
  "items": [
    {
      "productId": "product-guid",
      "finalPrice": 80000,
      "flashSaleQuantity": 20
    }
  ]
}
```

- **Output:** HTTP `201`, `Result.Success<FlashSaleDto>`; backend tính `discountPercent`. `flashSaleQuantity` phải nhỏ hơn tổng quantity sản phẩm và lịch không được chồng lấn với FlashSale khác của cùng sản phẩm.
- **BR/VAL:** `BR-BIZ-002`–`004`, `BR-BIZ-023`, `VAL-002`–`003`, `VAL-005`–`006`, `VAL-008`, `VAL-012`.

### STORE-11 — Cập nhật FlashSale

- **API:** `PUT /api/v1/admin/flash-sales/{flashSaleId}`
- **Input:** Path `flashSaleId: Guid`; body có cùng trường với STORE-10.
- **Output:** HTTP `200`, `Result.Success<FlashSaleDto>` sau cập nhật. Không sửa snapshot giá của đơn đã tạo; không giảm quota xuống thấp hơn lượng đã được đơn chờ xử lý giữ.
- **BR/VAL:** `BR-BIZ-002`–`004`, `BR-BIZ-006`, `BR-BIZ-023`, `VAL-002`–`003`, `VAL-005`–`006`, `VAL-008`, `VAL-012`.

### STORE-12 — Cập nhật trạng thái FlashSale hàng loạt

- **API:** `PATCH /api/v1/admin/flash-sales/batch-status`
- **Input body:** `ids: Guid[]`, `status: string`.

```json
{
  "ids": ["flash-sale-guid-1", "flash-sale-guid-2"],
  "status": "INACTIVE"
}
```

- **Output:** HTTP `200`, `Result.Success<BatchStatusDto>`. Một chương trình lỗi hoặc gây xung đột lịch thì rollback toàn bộ.
- **BR/VAL:** `BR-BIZ-004`, `BR-BIZ-011`, `BR-BIZ-023`, `VAL-006`, `VAL-009`–`010`.

### STORE-13 — Thiết lập giờ bắt đầu FlashSale

- **API:** `PATCH /api/v1/admin/flash-sales/{flashSaleId}/start-time`
- **Input:** Path `flashSaleId: Guid`; body:

```json
{
  "startsAt": "2026-11-07T03:00:00Z"
}
```

- **Output:** HTTP `200`, `Result.Success<{ flashSaleId, startsAt }>`.
- **BR/VAL:** `BR-BIZ-004`, `BR-BIZ-023`, `VAL-006`, `VAL-008`.

### STORE-14 — Danh sách đơn hàng

- **API:** `GET /api/v1/admin/orders`
- **Input query:** `searchUser?: string`, `status?: string`, `minTotalPrice?: decimal`, `maxTotalPrice?: decimal`, `sortDirection?: ASC|DESC`, `pageIndex?: int=1`, `pageSize?: int=10`.
- **Output:** HTTP `200`, `Result.Success<Page<OrderSummaryDto>>`; lọc trước, sau đó sắp xếp tổng tiền và phân trang.
- **BR/VAL:** `BR-BIZ-024`, `VAL-007`, `VAL-009`, `VAL-011`.

### STORE-15 — Xem chi tiết đơn hàng

- **API:** `GET /api/v1/admin/orders/{orderId}`
- **Input:** Path `orderId: Guid`.
- **Output:** HTTP `200`, `Result.Success<OrderDetailDto>` gồm khách hàng, dòng hàng theo giá snapshot, địa chỉ giao hàng và thông tin thanh toán.
- **BR/VAL:** `BR-BIZ-006`, `BR-BIZ-024`, `VAL-006`.

## 5. Ví dụ lỗi đại diện

Application trả lỗi nghiệp vụ bằng `Result.Failure<T>(Error)`. Presentation ánh xạ `Error.Code` sang HTTP status; body lỗi dùng `header` làm tên/mã lỗi và `description` làm message. Các status dưới đây là khóa response của API contract.

### List — CUS-06: query sort không hợp lệ

```json
{
  "400": {
    "description": "Yêu cầu không hợp lệ",
    "body": {
      "errors": [
        {
          "header": "INVALID_SORT_DIRECTION",
          "description": "sortByDiscount chỉ nhận ASC hoặc DESC."
        }
      ]
    },
    "applicationResult": {
      "isSuccess": false,
      "isFailure": true,
      "error": {
        "code": "Validation.InvalidSortDirection",
        "message": "sortByDiscount chỉ nhận ASC hoặc DESC."
      }
    }
  }
}
```

### Detail — CUS-08: không tìm thấy product

```json
{
  "404": {
    "description": "Không tìm thấy tài nguyên",
    "body": {
      "errors": [
        {
          "header": "PRODUCT_NOT_FOUND",
          "description": "Không tìm thấy sản phẩm."
        }
      ]
    },
    "applicationResult": {
      "isSuccess": false,
      "isFailure": true,
      "error": {
        "code": "Product.NotFound",
        "message": "Không tìm thấy sản phẩm."
      }
    }
  }
}
```

### Create — CUS-15: không đủ tồn kho

```json
{
  "409": {
    "description": "Xung đột tồn kho",
    "body": {
      "errors": [
        {
          "header": "OUT_OF_STOCK",
          "description": "Sản phẩm Áo thun không đủ số lượng yêu cầu."
        }
      ]
    },
    "applicationResult": {
      "isSuccess": false,
      "isFailure": true,
      "error": {
        "code": "Order.OutOfStock",
        "message": "Sản phẩm Áo thun không đủ số lượng yêu cầu."
      }
    }
  }
}
```

### Update — STORE-11: lịch FlashSale bị chồng lấn

```json
{
  "409": {
    "description": "Xung đột lịch FlashSale",
    "body": {
      "errors": [
        {
          "header": "OVERLAPPING_FLASH_SALE",
          "description": "Sản phẩm đã thuộc một FlashSale trong khoảng thời gian này."
        }
      ]
    },
    "applicationResult": {
      "isSuccess": false,
      "isFailure": true,
      "error": {
        "code": "FlashSale.TimeConflict",
        "message": "Sản phẩm đã thuộc một FlashSale trong khoảng thời gian này."
      }
    }
  }
}
```

### Batch status — STORE-08: một product không tồn tại

```json
{
  "404": {
    "description": "Batch không thể hoàn tất; toàn bộ thay đổi đã rollback",
    "body": {
      "errors": [
        {
          "header": "PRODUCT_NOT_FOUND",
          "description": "Không tìm thấy product-guid-2; không product nào được cập nhật."
        }
      ]
    },
    "applicationResult": {
      "isSuccess": false,
      "isFailure": true,
      "error": {
        "code": "Product.BatchUpdateFailed",
        "message": "Không tìm thấy product-guid-2; toàn bộ batch đã rollback."
      }
    }
  }
}
```

## 6. Điểm còn chờ xác nhận

- Enum phương thức thanh toán ngoài `ONLINE`.
- Cấu trúc chính thức của ảnh và thuộc tính product.
- Status HTTP tương ứng với từng nhóm `Error.Code` cần được triển khai trong `HandlerFailure`; hiện helper này đang map mọi `Result` thất bại thành `400`.
