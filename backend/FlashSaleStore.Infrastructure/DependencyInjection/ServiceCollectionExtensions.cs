using FlashSaleStore.Application.Abstractions.Payments;
using FlashSaleStore.Infrastructure.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlashSaleStore.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["PaymentGateway:PayOS:BaseUrl"] ?? "https://api-merchant.payos.vn";
        services.AddHttpClient<IPaymentGateway, PayOsPaymentGateway>(client => client.BaseAddress = new Uri(baseUrl));
        return services;
    }
}
