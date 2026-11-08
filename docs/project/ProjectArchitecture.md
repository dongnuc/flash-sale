# ProjectArchitecture

Tài liệu quy ước kiến trúc cho solution **.NET 8 – Minimal API – Clean Architecture**.
Mọi thành viên phải tuân thủ khi thêm code mới.

---

## 1. Cấu trúc solution

| Project | Vai trò |
|---|---|
| **Domain** | Entity, Value Object, Domain Event, Enum, Domain Exception. Chứa nghiệp vụ cốt lõi. |
| **Application** | Use case (Command/Query), DTO, **định nghĩa interface** (repository, query, service), validation, mapping. |
| **Persistence** | **Triển khai** interface truy cập DB bằng raw SQL, connection factory, transaction/unit of work. |
| **Infrastructure** | **Triển khai** các dịch vụ ngoài DB: email, cache, file storage, HTTP client, clock, JWT... |
| **Presentation** | Endpoint definitions (Minimal API), request/response contract, endpoint mapping. |
| **API** | Composition root: `Program.cs`, cấu hình, đăng ký DI, middleware. Là host chạy ứng dụng. |

---

## 2. Quy tắc tham chiếu giữa các project

Nguyên tắc: **phụ thuộc chỉ đi từ ngoài vào trong**. Tầng trong không biết gì về tầng ngoài.

```
                 ┌───────────────┐
                 │      API      │  (composition root)
                 └──┬──┬──┬──┬───┘
                    │  │  │  │
        ┌───────────┘  │  │  └────────────┐
        ▼              ▼  ▼               ▼
  Presentation   Infrastructure  Persistence   Application
        │              │          │               │
        └──────────────┴────┬─────┘               │
                            ▼                     ▼
                       Application ───────────► Domain
```

### Bảng tham chiếu cho phép

| Project | Được tham chiếu | KHÔNG được tham chiếu |
|---|---|---|
| Domain | (không project nào) | Tất cả |
| Application | Domain | Persistence, Infrastructure, Presentation, API |
| Persistence | Application, Domain | Infrastructure, Presentation, API |
| Infrastructure | Application, Domain | Persistence, Presentation, API |
| Presentation | Application | Persistence, Infrastructure, Domain (trực tiếp nên tránh) |
| API | Presentation, Application, Infrastructure, Persistence | — |

**Lưu ý quan trọng**
- Persistence và Infrastructure **được phép** tham chiếu Application, vì tầng ngoài tham chiếu tầng trong là đúng hướng.
- Persistence và Infrastructure **không tham chiếu lẫn nhau**.
- API tham chiếu Persistence/Infrastructure **chỉ để đăng ký DI**. Không gọi trực tiếp class trong đó.

---

## 3. Interface định nghĩa ở đâu, triển khai ở đâu

Quy tắc: **tầng trong định nghĩa interface (cái nó cần), tầng ngoài triển khai.**

| Interface | Định nghĩa tại | Triển khai tại |
|---|---|---|
| `IXxxRepository` (ghi, làm việc với Entity) | Application (`Abstractions/Repositories`) | Persistence (`Repositories`) |
| `IXxxQueries` (đọc, trả DTO) | Application (`Abstractions/Queries`) | Persistence (`Queries`) |
| `IUnitOfWork`, `IDbConnectionFactory` | Application (`Abstractions/Data`) | Persistence |
| `IEmailSender`, `ICacheService`, `IFileStorage` | Application (`Abstractions/Services`) | Infrastructure |
| `IDateTimeProvider`, `ICurrentUser`, `ITokenService` | Application (`Abstractions/Services`) | Infrastructure |
| Interface chỉ thuần nghiệp vụ domain (domain service) | Domain | Domain hoặc Application |

> Quy tắc nhanh: nếu một use case cần "gọi ra ngoài" (DB, mail, cache, API khác) thì khai báo interface ở **Application**, còn code thật nằm ở **Persistence** hoặc **Infrastructure**.

---

## 4. Entity, DTO và mapping

### 4.1 Phía ghi (Command): dùng Entity

- Repository nhận và trả **Entity/Aggregate** của Domain.
- Persistence đọc dữ liệu từ SQL rồi **dựng lại Entity** qua constructor hoặc factory method của Domain.
- Không để Entity phơi `public set` chỉ để phục vụ mapping. Dùng constructor/factory hoặc row model nội bộ.

### 4.2 Phía đọc (Query): dùng DTO / Read model

- Query trả thẳng **DTO** từ SQL, **không đi qua Entity**.
- Không nạp Entity chỉ để map sang DTO cho màn hình danh sách/báo cáo.

### 4.3 DTO đặt ở đâu

- DTO định nghĩa **một lần duy nhất ở Application**, cạnh use case/query dùng nó.
- Persistence tham chiếu Application nên dùng lại DTO này khi map kết quả SQL.
- **Không viết DTO hai lần** cho use case và Persistence.

### 4.4 Row model nội bộ (tuỳ chọn)

Chỉ tạo khi shape của bảng lệch nhiều so với DTO/Entity:
- Khai báo `internal` trong Persistence.
- Chỉ dùng trong Persistence, không lộ ra ngoài.
- Map từ row model sang Entity hoặc DTO ngay trong Persistence.

### 4.5 Contract của Presentation

- `Request`/`Response` của endpoint thuộc **Presentation**.
- Endpoint map `Request` -> `Command/Query` (Application), và `DTO` -> `Response` khi cần che bớt/đổi shape.
- Nếu DTO của Application đã đủ dùng làm response thì có thể trả thẳng, không bắt buộc tạo Response riêng.

---

## 5. Cấu trúc thư mục gợi ý

```
Domain/
  Common/                      // BaseEntity, ValueObject, DomainException
  Orders/
    Order.cs
    OrderItem.cs
    OrderStatus.cs

Application/
  Abstractions/
    Data/                      // IDbConnectionFactory, IUnitOfWork
    Repositories/              // IOrderRepository
    Queries/                   // IOrderQueries
    Services/                  // IEmailSender, ICacheService, IDateTimeProvider
  Common/                      // Result, Error, PagedResult, Behaviors
  Orders/
    Commands/
      CreateOrder/
        CreateOrderCommand.cs
        CreateOrderCommandHandler.cs
        CreateOrderCommandValidator.cs
    Queries/
      GetOrderSummaries/
        GetOrderSummariesQuery.cs
        GetOrderSummariesQueryHandler.cs
        OrderSummaryDto.cs
  DependencyInjection.cs       // AddApplication()

Persistence/
  Data/
    DbConnectionFactory.cs
    UnitOfWork.cs
  Repositories/
    OrderRepository.cs         // raw SQL, dựng lại Entity
  Queries/
    OrderQueries.cs            // raw SQL, trả DTO
  Rows/                        // row model internal (nếu cần)
  DependencyInjection.cs       // AddPersistence()

Infrastructure/
  Email/
  Caching/
  Time/
  DependencyInjection.cs       // AddInfrastructure()

Presentation/
  Endpoints/
    OrderEndpoints.cs          // MapOrderEndpoints()
  Contracts/
    Orders/CreateOrderRequest.cs
  DependencyInjection.cs       // AddPresentation()

API/
  Program.cs
  appsettings.json
```

---

## 6. Đăng ký DI

Mỗi project ngoài cùng chỉ lộ **một** extension method. Class triển khai để `internal`.

```csharp
// Persistence/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IDbConnectionFactory>(
            _ => new DbConnectionFactory(config.GetConnectionString("Default")!));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderQueries, OrderQueries>();
        return services;
    }
}
```

```csharp
// API/Program.cs
builder.Services
    .AddApplication()
    .AddPersistence(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddPresentation();

var app = builder.Build();
app.MapEndpoints();   // gom các MapXxxEndpoints() từ Presentation
app.Run();
```

---

## 7. Quy tắc viết raw SQL trong Persistence

1. **Luôn dùng tham số** (`@Id`, `@Name`...). Cấm nối chuỗi từ input.
2. SQL để dạng `const string` ở đầu method, dùng raw string literal (`"""`) cho query dài.
3. **Alias cột khớp tên property** của DTO/row model, hoặc cấu hình mapping tập trung một chỗ.
4. **Không `SELECT *`**. Liệt kê cột cần dùng.
5. Phân trang bắt buộc với query danh sách (`OFFSET/FETCH` hoặc `LIMIT/OFFSET` tuỳ DB).
6. Truyền `CancellationToken` vào mọi method bất đồng bộ.
7. Mở connection qua `IDbConnectionFactory`, dùng `using`/`await using`. Không giữ connection dài hạn.
8. Nhiều thao tác ghi liên quan nhau phải nằm trong **một transaction** (qua `IUnitOfWork`).
9. Không đặt logic nghiệp vụ trong SQL hoặc repository. Repository chỉ lưu/đọc.
10. Mỗi repository/query class phụ trách **một aggregate hoặc một nhóm màn hình**, tránh class "God".

---

## 8. Quy tắc code chung

### Domain
- Không dùng thư viện hạ tầng (không Dapper, không `HttpClient`, không `IConfiguration`).
- Entity bảo vệ bất biến bằng constructor/method, setter `private`.
- Lỗi nghiệp vụ ném `DomainException` hoặc trả `Result`, không dùng exception chung chung.

### Application
- Mỗi use case là một Command/Query + Handler riêng.
- Handler chỉ điều phối: gọi repository/service qua interface, gọi logic Domain.
- Validation đầu vào đặt ở Validator (ví dụ FluentValidation), không trong endpoint.
- Không tham chiếu `HttpContext`, `IResult`, hay bất cứ thứ gì của ASP.NET.
- Giá trị trả về thống nhất dạng `Result<T>` hoặc exception có kiểm soát (chọn **một** cách cho cả dự án).

### Persistence / Infrastructure
- Class triển khai để `internal sealed`.
- Không để lộ kiểu của thư viện bên thứ ba (ví dụ `SqlConnection`) ra interface của Application.
- Không chứa nghiệp vụ.

### Presentation
- Endpoint **mỏng**: nhận request, gửi Command/Query, trả kết quả. Không logic nghiệp vụ.
- Mỗi nhóm tài nguyên một file `XxxEndpoints.cs` với `MapXxxEndpoints()`.
- Dùng `MapGroup`, đặt tên endpoint (`WithName`), khai báo `Produces`/`WithTags` đầy đủ.
- Chuyển `Result`/lỗi sang `ProblemDetails` ở **một nơi** (extension hoặc exception handler).

### API
- `Program.cs` chỉ gọi các `AddXxx()` và `MapXxx()`. Không viết nghiệp vụ.
- Cấu hình (connection string, key) đọc từ `appsettings`/biến môi trường/secret, không hardcode.

---

## 9. Quy ước đặt tên

| Loại | Quy ước | Ví dụ |
|---|---|---|
| Command | `<Verb><Noun>Command` | `CreateOrderCommand` |
| Query | `Get<Noun>Query` | `GetOrderSummariesQuery` |
| Handler | `<Command/Query>Handler` | `CreateOrderCommandHandler` |
| DTO | `<Noun><Purpose>Dto` | `OrderSummaryDto` |
| Request/Response | `<Verb><Noun>Request/Response` | `CreateOrderRequest` |
| Repository | `I<Aggregate>Repository` | `IOrderRepository` |
| Query service | `I<Area>Queries` | `IOrderQueries` |
| Endpoint file | `<Resource>Endpoints` | `OrderEndpoints` |
| Method async | hậu tố `Async` | `GetByIdAsync` |

---

## 10. Quy trình thêm một tính năng mới

1. **Domain**: thêm/sửa Entity, Value Object nếu có nghiệp vụ mới.
2. **Application**:
   - Khai báo interface cần thiết (`IXxxRepository` / `IXxxQueries`) nếu chưa có.
   - Tạo Command/Query, Handler, Validator, DTO.
3. **Persistence/Infrastructure**: triển khai interface vừa khai báo, đăng ký trong `AddPersistence()`/`AddInfrastructure()`.
4. **Presentation**: thêm Request/Response (nếu cần) và endpoint.
5. **API**: kiểm tra đã gọi `MapXxxEndpoints()`.
6. Viết test: unit test cho Domain/Handler, integration test cho Persistence.

---

## 11. Checklist review PR

- [ ] Không có tham chiếu project sai hướng (kiểm tra file `.csproj`).
- [ ] Interface nằm ở Application, implementation nằm ở Persistence/Infrastructure.
- [ ] DTO chỉ định nghĩa một lần, đặt ở Application.
- [ ] Phía đọc trả DTO, phía ghi dùng Entity.
- [ ] SQL có tham số, không `SELECT *`, query danh sách có phân trang.
- [ ] Endpoint mỏng, không chứa nghiệp vụ.
- [ ] Class triển khai là `internal`, chỉ lộ `AddXxx()`.
- [ ] Có `CancellationToken` ở mọi method async.
- [ ] Không hardcode connection string/secret.