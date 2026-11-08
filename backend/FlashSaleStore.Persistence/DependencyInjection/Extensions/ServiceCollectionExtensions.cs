using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FlashSaleStore.Application.Abstractions.Data;
using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Persistence.Connection;
using FlashSaleStore.Persistence.Options;
using FlashSaleStore.Persistence.Queries;
using FlashSaleStore.Persistence.Repositories;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.DependencyInjection.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseOptions.DefaultConnectionName)
                               ?? configuration.GetSection($"{DatabaseOptions.ConnectionStringsSection}:{DatabaseOptions.DefaultConnectionName}").Value;

        services.Configure<DatabaseOptions>(options =>
        {
            options.ConnectionString = connectionString ?? string.Empty;
        });

        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<ISqlExecutor, SqlExecutor>();
        services.AddScoped<UnitOfWork.UnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<UnitOfWork.UnitOfWork>());
        services.AddScoped<ISqlTransactionAccessor>(provider => provider.GetRequiredService<UnitOfWork.UnitOfWork>());

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IFlashSaleRepository, FlashSaleRepository>();
        services.AddScoped<IFlashSaleItemRepository, FlashSaleItemRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartItemRepository, CartItemRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserClaimRepository, UserClaimRepository>();
        services.AddScoped<IRoleClaimRepository, RoleClaimRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IUserLoginRepository, UserLoginRepository>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();

        services.AddScoped<ICategoryQueries, CategoryQueries>();
        services.AddScoped<IProductQueries, ProductQueries>();
        services.AddScoped<IFlashSaleQueries, FlashSaleQueries>();
        services.AddScoped<ICartQueries, CartQueries>();
        services.AddScoped<IOrderQueries, OrderQueries>();

        return services;
    }
}
