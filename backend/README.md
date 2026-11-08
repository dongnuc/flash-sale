# FlashSaleStore Backend

## Run the API

Configure the database and secrets through environment variables or a local secret store. Do not commit credentials:

- `ConnectionStrings__DefaultConnection`: SQL Server connection string for the deployed FlashSaleStore schema.
- `Authentication__Jwt__SigningKey`: signing key for bearer tokens; use at least 32 bytes.
- `Authentication__Jwt__Issuer` and `Authentication__Jwt__Audience`: optional token issuer and audience checks.
- `PaymentGateway__PayOS__ClientId`, `PaymentGateway__PayOS__ApiKey`, `PaymentGateway__PayOS__ChecksumKey`, `PaymentGateway__PayOS__ReturnUrl`, and `PaymentGateway__PayOS__CancelUrl`: PayOS checkout configuration.
- `BackgroundJobs__OrderExpiry__IntervalSeconds` and `BackgroundJobs__OrderExpiry__BatchSize`: optional unpaid-order worker settings.

Apply the additive SQL migrations under [`../docs/backend/Migrations`](../docs/backend/Migrations) before running the API against an existing database. A new database can be created from [`../docs/backend/FlashSaleStore-Schema.sql`](../docs/backend/FlashSaleStore-Schema.sql).

## Run the tests

The tests include xUnit facts for CI and an in-process runner that avoids a testhost process, which is useful in restricted environments:

```powershell
dotnet run --project FlashSaleStore.Tests/FlashSaleStore.Tests.csproj
dotnet test FlashSaleStore.Tests/FlashSaleStore.Tests.csproj
```

The current suite covers catalog/cart validators, customer/admin query scoping, FlashSale pricing and schedule rules, order inventory reservation/expiry, late-payment manual-refund behavior, and PayOS request/webhook signatures. Database-backed API tests require an isolated SQL Server test database and are not pointed at the configured store database by default.
