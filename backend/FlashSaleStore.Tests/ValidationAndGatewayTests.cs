using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlashSaleStore.Application.Abstractions.Payments;
using FlashSaleStore.Application.UseCases.Cart;
using FlashSaleStore.Application.UseCases.FlashSales.Commands;
using FlashSaleStore.Application.UseCases.Orders;
using FlashSaleStore.Application.UseCases.Catalog.Categories.Commands;
using FlashSaleStore.Application.UseCases.Catalog.Products.Commands;
using FlashSaleStore.Infrastructure.Payments;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FlashSaleStore.Tests;

public sealed class ValidationAndGatewayTests
{
    [Fact]
    public void Catalog_and_cart_validators_accept_valid_inputs_and_reject_invalid_values()
    {
        Assert.True(new CreateCategoryCommandValidator().Validate(new CreateCategoryCommand("Books", "desc")).IsValid);
        Assert.False(new CreateProductCommandValidator().Validate(new CreateProductCommand(Guid.NewGuid(), "Item", null, 0, 0, false, "ACTIVE")).IsValid);
        Assert.True(new AddCartItemCommandValidator().Validate(new AddCartItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1)).IsValid);
        Assert.False(new SetCartItemQuantityCommandValidator().Validate(new SetCartItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 0)).IsValid);
    }

    [Fact]
    public void Flash_sale_checkout_and_admin_order_validators_check_required_enums_and_periods()
    {
        var start = DateTime.UtcNow.AddHours(1);
        var valid = new CreateFlashSaleCommand("Sale", start, start.AddHours(1), [new(Guid.NewGuid(), 80m, 2)]);
        var invalid = valid with { EndsAt = start };
        Assert.True(new CreateFlashSaleCommandValidator().Validate(valid).IsValid);
        Assert.False(new CreateFlashSaleCommandValidator().Validate(invalid).IsValid);

        var order = new CreateOrderCommand(Guid.NewGuid(), "ONLINE", new("A", "0900000000", "Province", "District", "Ward", "Address"));
        Assert.True(new CreateOrderCommandValidator().Validate(order).IsValid);
        Assert.False(new CreateOrderCommandValidator().Validate(order with { PaymentMethod = "COD" }).IsValid);
        Assert.False(new GetAdminOrdersQueryValidator().Validate(new GetAdminOrdersQuery(null, "999", null, null, "SIDEWAYS", 1, 10)).IsValid);
    }

    [Fact]
    public async Task Payos_webhook_is_accepted_only_when_signature_matches_signed_data()
    {
        const string checksum = "test-checksum-key";
        var gateway = CreateGateway(checksum, new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var signedData = new SortedDictionary<string, object>(StringComparer.Ordinal)
        {
            ["amount"] = 7000,
            ["code"] = "00",
            ["currency"] = "VND",
            ["desc"] = "Thành công",
            ["description"] = "FS 12345",
            ["orderCode"] = 12345,
            ["reference"] = "TX-1",
            ["transactionDateTime"] = "2026-11-08 12:00:00"
        };
        var canonical = string.Join("&", signedData.Select(pair => $"{pair.Key}={pair.Value}"));
        var signature = Hmac(canonical, checksum);
        var payload = JsonSerializer.Serialize(new { code = "00", desc = "success", success = true, data = signedData, signature });

        var verified = await gateway.VerifyWebhookAsync("PayOS", payload);
        var tampered = await gateway.VerifyWebhookAsync("PayOS", payload.Replace("7000", "7001", StringComparison.Ordinal));

        Assert.NotNull(verified);
        Assert.True(verified!.IsSuccess);
        Assert.Equal(12345, verified.OrderCode);
        Assert.Equal("TX-1", verified.TransactionId);
        Assert.Null(tampered);
    }

    [Fact]
    public async Task Payos_checkout_creates_signed_request_and_returns_provider_link()
    {
        const string checksum = "test-checksum-key";
        string? capturedRequest = null;
        string? capturedClientId = null;
        var handler = new StubHttpHandler(request =>
        {
            capturedClientId = request.Headers.GetValues("x-client-id").Single();
            capturedRequest = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"code":"00","desc":"success","data":{"paymentLinkId":"link-1","checkoutUrl":"https://pay.example/link-1","qrCode":"qr"}}""")
            };
        });
        var gateway = CreateGateway(checksum, handler);
        var expires = new DateTime(2026, 11, 8, 12, 30, 0, DateTimeKind.Utc);

        var result = await gateway.CreateCheckoutAsync(new(12345, 7000m, expires));
        using var json = JsonDocument.Parse(capturedRequest!);

        Assert.Equal("https://pay.example/link-1", result.CheckoutUrl);
        Assert.Equal("link-1", result.PaymentLinkId);
        Assert.Equal("x-client", capturedClientId);
        Assert.Equal(Hmac("amount=7000&cancelUrl=https://example.test/cancel&description=FlashSale 12345&orderCode=12345&returnUrl=https://example.test/return", checksum), json.RootElement.GetProperty("signature").GetString());
    }

    private static PayOsPaymentGateway CreateGateway(string checksumKey, HttpMessageHandler handler)
    {
        var values = new Dictionary<string, string?>
        {
            ["PaymentGateway:PayOS:ClientId"] = "x-client",
            ["PaymentGateway:PayOS:ApiKey"] = "x-api",
            ["PaymentGateway:PayOS:ChecksumKey"] = checksumKey,
            ["PaymentGateway:PayOS:ReturnUrl"] = "https://example.test/return",
            ["PaymentGateway:PayOS:CancelUrl"] = "https://example.test/cancel"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new PayOsPaymentGateway(new HttpClient(handler) { BaseAddress = new Uri("https://api-merchant.payos.vn") }, configuration);
    }

    private static string Hmac(string value, string key) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(callback(request));
    }
}
