/* =====================================================================
   FlashSaleStore - SCHEMA HOÀN CHỈNH (chạy trên database mới, SQL Server 2022 trở lên)

   Quy ước
   - 1 cửa hàng duy nhất. Role: Admin, StoreOwner (chủ cửa hàng), Customer (người dùng).
   - Mọi cột thời gian là UTC, kiểu datetime2(3), tên CreatedAt/UpdatedAt/... (không hậu tố Utc).
     Ngoại lệ: Users.LockoutEnd (datetimeoffset) vì ASP.NET Identity yêu cầu.
   - Khóa chính GUID do app sinh (khuyến nghị UUIDv7). Nếu app không truyền Id thì
     DEFAULT NEWSEQUENTIALID() sẽ sinh giá trị tuần tự.
   - Status lưu dạng SỐ (tinyint) khớp với enum C#, giới hạn bằng CHECK.
     Khai báo enum trong code đúng thứ tự dưới đây (giá trị mặc định 0,1,2,... của C#):
       OrderStatus     : Pending=0, Paid=1, Shipping=2, Completed=3, Cancelled=4, Expired=5
       PaymentStatus   : Pending=0, Paid=1, Failed=2, Expired=3, Cancelled=4
       FlashSaleStatus : Draft=0, Scheduled=1, Active=2, Ended=3, Cancelled=4
     Thêm giá trị enum mới thì phải sửa CHECK tương ứng (CK_*_Status) ở file này.
   - Trừ kho phải dùng UPDATE atomic (xem phần 13), không đọc rồi ghi.
   - Không có __EFMigrationsHistory: file này là nguồn schema. Nếu muốn dùng EF migrations
     thì phải tạo baseline từ schema này.
   ===================================================================== */

/* Muốn chạy lại từ đầu (XÓA TOÀN BỘ DỮ LIỆU), bỏ comment 3 dòng dưới:
USE [master]; ALTER DATABASE [FlashSaleStore] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [FlashSaleStore];
GO */

USE [master];
GO
IF DB_ID(N'FlashSaleStore') IS NULL
    CREATE DATABASE [FlashSaleStore];
GO
ALTER DATABASE [FlashSaleStore] SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE [FlashSaleStore] SET AUTO_CLOSE OFF;
ALTER DATABASE [FlashSaleStore] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
ALTER DATABASE [FlashSaleStore] SET RECOVERY SIMPLE;   -- môi trường dev; production nên FULL + backup log
GO
USE [FlashSaleStore];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- bắt buộc cho filtered index
GO

/* =====================================================================
   1. IDENTITY: Roles, Users và các bảng phụ (tương thích ASP.NET Core Identity, key Guid)
   ===================================================================== */
CREATE TABLE dbo.Roles (
    Id               uniqueidentifier NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    Name             nvarchar(256)    NOT NULL,
    NormalizedName   nvarchar(256)    NOT NULL,
    ConcurrencyStamp nvarchar(256)    NULL
);
CREATE UNIQUE INDEX RoleNameIndex ON dbo.Roles (NormalizedName);
GO

CREATE TABLE dbo.Users (
    Id                   uniqueidentifier NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    FullName             nvarchar(200)    NOT NULL,
    IsActive             bit              NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    UserName             nvarchar(256)    NOT NULL,
    NormalizedUserName   nvarchar(256)    NOT NULL,
    Email                nvarchar(256)    NOT NULL,
    NormalizedEmail      nvarchar(256)    NOT NULL,
    EmailConfirmed       bit              NOT NULL CONSTRAINT DF_Users_EmailConfirmed DEFAULT 0,
    PasswordHash         nvarchar(512)    NULL,
    SecurityStamp        nvarchar(256)    NULL,
    ConcurrencyStamp     nvarchar(256)    NULL,
    PhoneNumber          nvarchar(30)     NULL,
    PhoneNumberConfirmed bit              NOT NULL CONSTRAINT DF_Users_PhoneConfirmed DEFAULT 0,
    TwoFactorEnabled     bit              NOT NULL CONSTRAINT DF_Users_TwoFactor DEFAULT 0,
    LockoutEnd           datetimeoffset(7) NULL,
    LockoutEnabled       bit              NOT NULL CONSTRAINT DF_Users_LockoutEnabled DEFAULT 1,
    AccessFailedCount    int              NOT NULL CONSTRAINT DF_Users_AccessFailed DEFAULT 0,
    CreatedAt            datetime2(3)     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt            datetime2(3)     NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT SYSUTCDATETIME()
);
CREATE UNIQUE INDEX UserNameIndex ON dbo.Users (NormalizedUserName);
CREATE UNIQUE INDEX EmailIndex    ON dbo.Users (NormalizedEmail);   -- email là duy nhất
GO

CREATE TABLE dbo.UserRoles (
    UserId uniqueidentifier NOT NULL,
    RoleId uniqueidentifier NOT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE
);
CREATE INDEX IX_UserRoles_RoleId ON dbo.UserRoles (RoleId);

CREATE TABLE dbo.RoleClaims (
    Id         int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RoleClaims PRIMARY KEY,
    RoleId     uniqueidentifier  NOT NULL,
    ClaimType  nvarchar(256)     NULL,
    ClaimValue nvarchar(2000)    NULL,
    CONSTRAINT FK_RoleClaims_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE
);
CREATE INDEX IX_RoleClaims_RoleId ON dbo.RoleClaims (RoleId);

CREATE TABLE dbo.UserClaims (
    Id         int IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserClaims PRIMARY KEY,
    UserId     uniqueidentifier  NOT NULL,
    ClaimType  nvarchar(256)     NULL,
    ClaimValue nvarchar(2000)    NULL,
    CONSTRAINT FK_UserClaims_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
);
CREATE INDEX IX_UserClaims_UserId ON dbo.UserClaims (UserId);

CREATE TABLE dbo.UserLogins (
    LoginProvider       nvarchar(450)    NOT NULL,
    ProviderKey         nvarchar(450)    NOT NULL,
    ProviderDisplayName nvarchar(256)    NULL,
    UserId              uniqueidentifier NOT NULL,
    CONSTRAINT PK_UserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_UserLogins_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
);
CREATE INDEX IX_UserLogins_UserId ON dbo.UserLogins (UserId);

CREATE TABLE dbo.UserTokens (
    UserId        uniqueidentifier NOT NULL,
    LoginProvider nvarchar(450)    NOT NULL,
    Name          nvarchar(450)    NOT NULL,
    Value         nvarchar(max)    NULL,
    CONSTRAINT PK_UserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_UserTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
);
GO

/* =====================================================================
   2. SEED ROLES (Id cố định để code tham chiếu được)
      Tài khoản Admin đầu tiên phải tạo bằng code (UserManager) vì cần băm mật khẩu.
   ===================================================================== */
INSERT INTO dbo.Roles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES
 ('E1A00000-0000-4000-8000-000000000001', N'Admin',      N'ADMIN',      CONVERT(nvarchar(36), NEWID())),
 ('E1A00000-0000-4000-8000-000000000002', N'StoreOwner', N'STOREOWNER', CONVERT(nvarchar(36), NEWID())),
 ('E1A00000-0000-4000-8000-000000000003', N'Customer',   N'CUSTOMER',   CONVERT(nvarchar(36), NEWID()));
GO

/* =====================================================================
   3. Categories
   ===================================================================== */
CREATE TABLE dbo.Categories (
    Id          uniqueidentifier NOT NULL CONSTRAINT PK_Categories PRIMARY KEY CONSTRAINT DF_Categories_Id DEFAULT NEWSEQUENTIALID(),
    Name        nvarchar(150)    NOT NULL,
    Description nvarchar(500)    NULL,
    IsActive    bit              NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1,
    CreatedAt   datetime2(3)     NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   datetime2(3)     NOT NULL CONSTRAINT DF_Categories_UpdatedAt DEFAULT SYSUTCDATETIME()
);
CREATE UNIQUE INDEX UX_Categories_Name ON dbo.Categories (Name);
GO

/* =====================================================================
   4. Products
   ===================================================================== */
CREATE TABLE dbo.Products (
    Id            uniqueidentifier NOT NULL CONSTRAINT PK_Products PRIMARY KEY CONSTRAINT DF_Products_Id DEFAULT NEWSEQUENTIALID(),
    CategoryId    uniqueidentifier NOT NULL,
    Name          nvarchar(200)    NOT NULL,
    Description   nvarchar(1000)   NULL,
    Price         decimal(18,2)    NOT NULL,
    StockQuantity int              NOT NULL CONSTRAINT DF_Products_Stock DEFAULT 0,
    IsActive      bit              NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
    IsFeatured    bit              NOT NULL CONSTRAINT DF_Products_IsFeatured DEFAULT 0,
    CreatedAt     datetime2(3)     NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt     datetime2(3)     NOT NULL CONSTRAINT DF_Products_UpdatedAt DEFAULT SYSUTCDATETIME(),
    RowVersion    rowversion       NOT NULL,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id),
    CONSTRAINT CK_Products_StockPrice CHECK (StockQuantity >= 0 AND Price > 0)
);
CREATE INDEX IX_Products_CategoryId_IsActive ON dbo.Products (CategoryId, IsActive);
GO

/* =====================================================================
   5. FlashSales
   ===================================================================== */
CREATE TABLE dbo.FlashSales (
    Id        uniqueidentifier NOT NULL CONSTRAINT PK_FlashSales PRIMARY KEY CONSTRAINT DF_FlashSales_Id DEFAULT NEWSEQUENTIALID(),
    Name      nvarchar(200)    NOT NULL,
    StartsAt  datetime2(3)     NOT NULL,
    EndsAt    datetime2(3)     NOT NULL,
    Status    tinyint          NOT NULL CONSTRAINT DF_FlashSales_Status DEFAULT 0,   -- FlashSaleStatus.Draft
    CreatedAt datetime2(3)     NOT NULL CONSTRAINT DF_FlashSales_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2(3)     NOT NULL CONSTRAINT DF_FlashSales_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_FlashSales_Time   CHECK (EndsAt > StartsAt),
    CONSTRAINT CK_FlashSales_Status CHECK (Status BETWEEN 0 AND 4)   -- Draft..Cancelled
);
CREATE INDEX IX_FlashSales_Status_Time ON dbo.FlashSales (Status, StartsAt, EndsAt);
GO

/* =====================================================================
   6. FlashSaleItems
   ===================================================================== */
CREATE TABLE dbo.FlashSaleItems (
    Id           uniqueidentifier NOT NULL CONSTRAINT PK_FlashSaleItems PRIMARY KEY CONSTRAINT DF_FlashSaleItems_Id DEFAULT NEWSEQUENTIALID(),
    FlashSaleId  uniqueidentifier NOT NULL,
    ProductId    uniqueidentifier NOT NULL,
    SalePrice    decimal(18,2)    NOT NULL,
    SaleQuantity int              NOT NULL,
    SoldQuantity int              NOT NULL CONSTRAINT DF_FlashSaleItems_Sold DEFAULT 0,
    LimitPerUser int              NULL,    -- NULL = không giới hạn
    CreatedAt    datetime2(3)     NOT NULL CONSTRAINT DF_FlashSaleItems_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt    datetime2(3)     NOT NULL CONSTRAINT DF_FlashSaleItems_UpdatedAt DEFAULT SYSUTCDATETIME(),
    RowVersion   rowversion       NOT NULL,
    CONSTRAINT FK_FlashSaleItems_FlashSales FOREIGN KEY (FlashSaleId) REFERENCES dbo.FlashSales (Id) ON DELETE CASCADE,
    CONSTRAINT FK_FlashSaleItems_Products   FOREIGN KEY (ProductId)   REFERENCES dbo.Products (Id),
    CONSTRAINT CK_FlashSaleItems_Qty   CHECK (SaleQuantity > 0 AND SoldQuantity >= 0 AND SoldQuantity <= SaleQuantity),
    CONSTRAINT CK_FlashSaleItems_Price CHECK (SalePrice > 0),
    CONSTRAINT CK_FlashSaleItems_Limit CHECK (LimitPerUser IS NULL OR LimitPerUser > 0)
);
CREATE UNIQUE INDEX UX_FlashSaleItems_Sale_Product ON dbo.FlashSaleItems (FlashSaleId, ProductId);
CREATE INDEX IX_FlashSaleItems_ProductId ON dbo.FlashSaleItems (ProductId);
GO

/* =====================================================================
   7. Carts, CartItems
   ===================================================================== */
CREATE TABLE dbo.Carts (
    Id        uniqueidentifier NOT NULL CONSTRAINT PK_Carts PRIMARY KEY CONSTRAINT DF_Carts_Id DEFAULT NEWSEQUENTIALID(),
    UserId    uniqueidentifier NOT NULL,
    CreatedAt datetime2(3)     NOT NULL CONSTRAINT DF_Carts_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2(3)     NOT NULL CONSTRAINT DF_Carts_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Carts_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
);
CREATE UNIQUE INDEX UX_Carts_UserId ON dbo.Carts (UserId);   -- mỗi user một giỏ

CREATE TABLE dbo.CartItems (
    Id        uniqueidentifier NOT NULL CONSTRAINT PK_CartItems PRIMARY KEY CONSTRAINT DF_CartItems_Id DEFAULT NEWSEQUENTIALID(),
    CartId    uniqueidentifier NOT NULL,
    ProductId uniqueidentifier NOT NULL,
    Quantity  int              NOT NULL,
    CreatedAt datetime2(3)     NOT NULL CONSTRAINT DF_CartItems_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2(3)     NOT NULL CONSTRAINT DF_CartItems_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CartItems_Carts    FOREIGN KEY (CartId)    REFERENCES dbo.Carts (Id) ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id),
    CONSTRAINT CK_CartItems_Qty CHECK (Quantity > 0)
);
CREATE UNIQUE INDEX UX_CartItems_Cart_Product ON dbo.CartItems (CartId, ProductId);
CREATE INDEX IX_CartItems_ProductId ON dbo.CartItems (ProductId);
GO

/* =====================================================================
   8. Orders
      Không xóa vật lý đơn hàng: dùng Status = Cancelled/Expired.
   ===================================================================== */
CREATE TABLE dbo.Orders (
    Id                     uniqueidentifier NOT NULL CONSTRAINT PK_Orders PRIMARY KEY CONSTRAINT DF_Orders_Id DEFAULT NEWSEQUENTIALID(),
    UserId                 uniqueidentifier NOT NULL,
    TotalAmount            decimal(18,2)    NOT NULL,
    Status                 tinyint          NOT NULL CONSTRAINT DF_Orders_Status DEFAULT 0,   -- OrderStatus.Pending
    IdempotencyKey         nvarchar(150)    NULL,
    IdempotencyRequestHash nvarchar(256)    NULL,
    ExpiresAt              datetime2(3)     NULL,   -- hạn giữ hàng khi chờ thanh toán
    ReceiverName           nvarchar(200)    NULL,
    ReceiverPhone          nvarchar(30)     NULL,
    ShippingAddress        nvarchar(500)    NULL,
    Province               nvarchar(100)    NULL,
    District               nvarchar(100)    NULL,
    Ward                   nvarchar(100)    NULL,
    AddressLine            nvarchar(300)    NULL,
    Note                   nvarchar(500)    NULL,
    CreatedAt              datetime2(3)     NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt              datetime2(3)     NOT NULL CONSTRAINT DF_Orders_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id),
    CONSTRAINT CK_Orders_Total  CHECK (TotalAmount >= 0),
    CONSTRAINT CK_Orders_Status CHECK (Status BETWEEN 0 AND 5)   -- Pending..Expired
);
-- Idempotency theo từng user
CREATE UNIQUE INDEX UX_Orders_UserId_IdempotencyKey ON dbo.Orders (UserId, IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;
CREATE INDEX IX_Orders_UserId_CreatedAt ON dbo.Orders (UserId, CreatedAt DESC);
-- Job quét đơn hết hạn
CREATE INDEX IX_Orders_Status_ExpiresAt ON dbo.Orders (Status, ExpiresAt) WHERE ExpiresAt IS NOT NULL;
GO

/* =====================================================================
   9. OrderDetails (snapshot tên và giá tại thời điểm mua)
   ===================================================================== */
CREATE TABLE dbo.OrderDetails (
    Id              uniqueidentifier NOT NULL CONSTRAINT PK_OrderDetails PRIMARY KEY CONSTRAINT DF_OrderDetails_Id DEFAULT NEWSEQUENTIALID(),
    OrderId         uniqueidentifier NOT NULL,
    FlashSaleItemId uniqueidentifier NULL,   -- NULL = mua giá thường
    ProductId       uniqueidentifier NOT NULL,
    ProductName     nvarchar(200)    NOT NULL,
    Quantity        int              NOT NULL,
    UnitPrice       decimal(18,2)    NOT NULL,
    LineTotal       decimal(18,2)    NOT NULL,
    CreatedAt       datetime2(3)     NOT NULL CONSTRAINT DF_OrderDetails_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       datetime2(3)     NOT NULL CONSTRAINT DF_OrderDetails_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_OrderDetails_Orders         FOREIGN KEY (OrderId)         REFERENCES dbo.Orders (Id),
    CONSTRAINT FK_OrderDetails_FlashSaleItems FOREIGN KEY (FlashSaleItemId) REFERENCES dbo.FlashSaleItems (Id),
    CONSTRAINT FK_OrderDetails_Products       FOREIGN KEY (ProductId)       REFERENCES dbo.Products (Id),
    CONSTRAINT CK_OrderDetails_Values CHECK (Quantity > 0 AND UnitPrice >= 0 AND LineTotal = Quantity * UnitPrice)
);
CREATE INDEX IX_OrderDetails_OrderId   ON dbo.OrderDetails (OrderId);
CREATE INDEX IX_OrderDetails_ProductId ON dbo.OrderDetails (ProductId);
-- Phục vụ kiểm tra LimitPerUser và thống kê flash sale
CREATE INDEX IX_OrderDetails_FlashSaleItemId ON dbo.OrderDetails (FlashSaleItemId)
    INCLUDE (OrderId, Quantity) WHERE FlashSaleItemId IS NOT NULL;
GO

/* =====================================================================
   10. Payments (PayOS)
   ===================================================================== */
CREATE TABLE dbo.Payments (
    Id                    uniqueidentifier NOT NULL CONSTRAINT PK_Payments PRIMARY KEY CONSTRAINT DF_Payments_Id DEFAULT NEWSEQUENTIALID(),
    OrderId               uniqueidentifier NOT NULL,
    UserId                uniqueidentifier NOT NULL,
    Amount                decimal(18,2)    NOT NULL,
    Provider              nvarchar(50)     NOT NULL CONSTRAINT DF_Payments_Provider DEFAULT N'PayOS',
    Status                tinyint          NOT NULL CONSTRAINT DF_Payments_Status DEFAULT 0,   -- PaymentStatus.Pending
    RequiresManualRefund  bit              NOT NULL CONSTRAINT DF_Payments_ManualRefund DEFAULT 0,
    PayOsOrderCode        bigint           NOT NULL,
    PayOsPaymentLinkId    nvarchar(255)    NULL,
    ProviderTransactionId nvarchar(100)    NULL,   -- mã giao dịch từ cổng, chống xử lý webhook trùng
    CheckoutUrl           nvarchar(2048)   NULL,
    QrCode                nvarchar(2048)   NULL,
    FailureReason         nvarchar(500)    NULL,
    CreatedAt             datetime2(3)     NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt             datetime2(3)     NOT NULL CONSTRAINT DF_Payments_UpdatedAt DEFAULT SYSUTCDATETIME(),
    PaidAt                datetime2(3)     NULL,
    FailedAt              datetime2(3)     NULL,
    ExpiredAt             datetime2(3)     NULL,
    CancelledAt           datetime2(3)     NULL,
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id),
    CONSTRAINT FK_Payments_Users  FOREIGN KEY (UserId)  REFERENCES dbo.Users (Id),
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Payments_Status CHECK (Status BETWEEN 0 AND 4)   -- Pending..Cancelled
);
CREATE UNIQUE INDEX UX_Payments_PayOsOrderCode ON dbo.Payments (PayOsOrderCode);
CREATE UNIQUE INDEX UX_Payments_ProviderTransactionId ON dbo.Payments (ProviderTransactionId) WHERE ProviderTransactionId IS NOT NULL;
-- Mỗi đơn chỉ có tối đa một payment đã thanh toán (retry tạo payment mới được phép)
CREATE UNIQUE INDEX UX_Payments_OrderId_Paid ON dbo.Payments (OrderId) WHERE Status = 1;   -- PaymentStatus.Paid
CREATE INDEX IX_Payments_OrderId ON dbo.Payments (OrderId);
CREATE INDEX IX_Payments_UserId  ON dbo.Payments (UserId);
GO

/* =====================================================================
   11. SystemSettings - chỗ để sau này Admin cấu hình (hiện KHÔNG có dữ liệu)
       Dạng key-value. Chưa dùng thì có thể bỏ nguyên phần này.
   ===================================================================== */
CREATE TABLE dbo.SystemSettings (
    SettingKey      nvarchar(100)    NOT NULL CONSTRAINT PK_SystemSettings PRIMARY KEY,
    SettingValue    nvarchar(2000)   NULL,
    Description     nvarchar(500)    NULL,
    UpdatedAt       datetime2(3)     NOT NULL CONSTRAINT DF_SystemSettings_UpdatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedByUserId uniqueidentifier NULL,
    CONSTRAINT FK_SystemSettings_Users FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users (Id)
);
GO

/* =====================================================================
   12. KIỂM TRA SAU KHI CHẠY
   ===================================================================== */
SELECT Id, Name, NormalizedName FROM dbo.Roles ORDER BY Name;
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;
GO

/* =====================================================================
   13. MẪU CÂU LỆNH QUAN TRỌNG (dùng trong Persistence, không chạy ở file này)
   ---------------------------------------------------------------------
   -- Trừ kho flash sale atomic (@@ROWCOUNT = 0 => hết hàng, rollback đơn)
   UPDATE dbo.FlashSaleItems
   SET SoldQuantity = SoldQuantity + @Qty, UpdatedAt = SYSUTCDATETIME()
   WHERE Id = @FlashSaleItemId AND SoldQuantity + @Qty <= SaleQuantity;

   -- Hoàn kho khi đơn Cancelled/Expired
   UPDATE dbo.FlashSaleItems
   SET SoldQuantity = SoldQuantity - @Qty, UpdatedAt = SYSUTCDATETIME()
   WHERE Id = @FlashSaleItemId AND SoldQuantity - @Qty >= 0;

   -- Trừ kho hàng thường
   UPDATE dbo.Products
   SET StockQuantity = StockQuantity - @Qty, UpdatedAt = SYSUTCDATETIME()
   WHERE Id = @ProductId AND StockQuantity >= @Qty;

   -- Số lượng user đã mua của một flash sale item (kiểm tra LimitPerUser)
   SELECT COALESCE(SUM(od.Quantity), 0)
   FROM dbo.OrderDetails od JOIN dbo.Orders o ON o.Id = od.OrderId
   WHERE od.FlashSaleItemId = @FlashSaleItemId AND o.UserId = @UserId
     AND o.Status IN (0, 1, 2, 3);   -- Pending, Paid, Shipping, Completed (bỏ Cancelled, Expired)
   ===================================================================== */
