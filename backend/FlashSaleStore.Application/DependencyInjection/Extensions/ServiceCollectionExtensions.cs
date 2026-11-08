using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using FlashSaleStore.Application.Behaviors;
using FlashSaleStore.Application.Mappers;
using FlashSaleStore.Domain.Services;
namespace FlashSaleStore.Application.DependencyInjection.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfigureMediatR(this IServiceCollection services)
        => services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly))
            .AddTransient(typeof(IPipelineBehavior<,>),typeof(ValidationPipelineBehavior<,>))
            //.AddTransient(typeof(IPipelineBehavior<,>),typeof(PerformancePipelineBehavior<,>));
            //.AddTransient(typeof(IPipelineBehavior<,>),typeof(ValidationPipelineBehavior<,>));
            .AddValidatorsFromAssembly(typeof(AssemblyReference).Assembly, includeInternalTypes: true)
            .AddScoped<FlashSaleSchedulePolicy>()
            .AddScoped<FlashSalePricingService>()
            .AddScoped<FlashSaleStore.Application.UseCases.Orders.OrderInventoryReleaser>()
            ;

        public static IServiceCollection AddConfigureAutoMapper(this IServiceCollection services)
            => services.AddAutoMapper(typeof(ServiceProfile));
        // config AutoMapper
    }
}
