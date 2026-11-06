using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Movie_StructureCode.Application.Abstractions.Data;
using Movie_StructureCode.Persistence.Connection;
using Movie_StructureCode.Persistence.Options;
using Movie_StructureCode.Persistence.Sql;
using Movie_StructureCode.Persistence.UnitOfWork;

namespace Movie_StructureCode.Persistence.DependencyInjection.Extensions;

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
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

        return services;
    }
}
