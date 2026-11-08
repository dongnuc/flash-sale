using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlashSaleStore.Application.Abstractions.Payments;
using Microsoft.Extensions.Configuration;

namespace FlashSaleStore.Infrastructure.Payments;

public sealed class PayOsPaymentGateway(HttpClient httpClient, IConfiguration configuration) : IPaymentGateway
{
    private const string Section = "PaymentGateway:PayOS";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PaymentGatewayCheckout> CreateCheckoutAsync(PaymentGatewayCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var options = ReadOptions();
        EnsureConfigured(options);
        if (request.Amount != decimal.Truncate(request.Amount) || request.Amount > int.MaxValue)
            throw new PaymentGatewayException("Payment.Amount.Unsupported", "PayOS yêu cầu số tiền VND là số nguyên trong giới hạn int.");
        var amount = decimal.ToInt32(request.Amount);
        var description = $"FlashSale {request.OrderCode}";
        if (description.Length > 25) description = $"FS {request.OrderCode}";
        var dataToSign = $"amount={amount}&cancelUrl={options.CancelUrl}&description={description}&orderCode={request.OrderCode}&returnUrl={options.ReturnUrl}";
        var signature = Hmac(dataToSign, options.ChecksumKey!);
        var expiredAt = checked((int)new DateTimeOffset(DateTime.SpecifyKind(request.ExpiresAtUtc, DateTimeKind.Utc)).ToUnixTimeSeconds());
        using var message = new HttpRequestMessage(HttpMethod.Post, "/v2/payment-requests")
        {
            Content = JsonContent.Create(new
            {
                orderCode = request.OrderCode,
                amount,
                description,
                cancelUrl = options.CancelUrl,
                returnUrl = options.ReturnUrl,
                expiredAt,
                signature
            }, options: JsonOptions)
        };
        message.Headers.TryAddWithoutValidation("x-client-id", options.ClientId);
        message.Headers.TryAddWithoutValidation("x-api-key", options.ApiKey);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new PaymentGatewayException("Payment.Provider.Unavailable", $"PayOS từ chối tạo link thanh toán (HTTP {(int)response.StatusCode}).");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("code", out var code) || code.GetString() != "00" || !root.TryGetProperty("data", out var data))
            throw new PaymentGatewayException("Payment.Provider.Rejected", "PayOS không tạo được link thanh toán.");
        return new PaymentGatewayCheckout(
            data.TryGetProperty("paymentLinkId", out var linkId) ? linkId.GetString() ?? string.Empty : string.Empty,
            data.TryGetProperty("checkoutUrl", out var checkoutUrl) ? checkoutUrl.GetString() : null,
            data.TryGetProperty("qrCode", out var qrCode) ? qrCode.GetString() : null);
    }

    public Task<PaymentGatewayWebhook?> VerifyWebhookAsync(string provider, string rawBody, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(provider, "PayOS", StringComparison.OrdinalIgnoreCase))
            throw new PaymentGatewayException("Payment.Provider.Unsupported", "Nhà cung cấp thanh toán không được hỗ trợ.");
        var options = ReadOptions();
        EnsureConfigured(options);
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            if (!root.TryGetProperty("data", out var data) || !root.TryGetProperty("signature", out var signatureElement)) return Task.FromResult<PaymentGatewayWebhook?>(null);
            var signature = signatureElement.GetString();
            var canonical = string.Join("&", data.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => $"{property.Name}={ToCanonicalValue(property.Value)}"));
            var expected = Convert.FromHexString(Hmac(canonical, options.ChecksumKey!));
            var supplied = Convert.FromHexString(signature ?? string.Empty);
            if (!CryptographicOperations.FixedTimeEquals(expected, supplied)) return Task.FromResult<PaymentGatewayWebhook?>(null);
            var orderCode = data.GetProperty("orderCode").GetInt64();
            var amount = data.GetProperty("amount").GetDecimal();
            var success = root.TryGetProperty("success", out var successElement) && successElement.GetBoolean()
                          && data.TryGetProperty("code", out var transactionCode) && transactionCode.GetString() == "00";
            var transactionId = data.TryGetProperty("reference", out var reference) ? reference.GetString() : null;
            var reason = data.TryGetProperty("desc", out var desc) ? desc.GetString() : null;
            var now = DateTime.UtcNow;
            return Task.FromResult<PaymentGatewayWebhook?>(new PaymentGatewayWebhook(orderCode, amount, success,
                string.IsNullOrWhiteSpace(transactionId) ? $"PAYOS-{orderCode}" : transactionId,
                now, success ? null : reason ?? "Giao dịch bị từ chối."));
        }
        catch (FormatException) { return Task.FromResult<PaymentGatewayWebhook?>(null); }
        catch (JsonException) { return Task.FromResult<PaymentGatewayWebhook?>(null); }
        catch (KeyNotFoundException) { return Task.FromResult<PaymentGatewayWebhook?>(null); }
        catch (InvalidOperationException) { return Task.FromResult<PaymentGatewayWebhook?>(null); }
    }

    private PayOsOptions ReadOptions() => configuration.GetSection(Section).Get<PayOsOptions>() ?? new PayOsOptions();
    private void EnsureConfigured(PayOsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ApiKey) ||
            string.IsNullOrWhiteSpace(options.ChecksumKey) || string.IsNullOrWhiteSpace(options.ReturnUrl) ||
            string.IsNullOrWhiteSpace(options.CancelUrl))
            throw new PaymentGatewayException("Payment.Provider.NotConfigured", "PayOS chưa được cấu hình đầy đủ.");
        if (httpClient.BaseAddress is null && Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)) httpClient.BaseAddress = baseUri;
    }
    private static string Hmac(string value, string key) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string ToCanonicalValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        JsonValueKind.Array or JsonValueKind.Object => JsonSerializer.Serialize(value, JsonOptions),
        _ => value.GetRawText()
    };
}
