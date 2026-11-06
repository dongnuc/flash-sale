using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Movie_StructureCode.Application.Behaviors;
using Movie_StructureCode.Application.Mappers;
namespace Movie_StructureCode.Application.DependencyInjection.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfigureMediatR(this IServiceCollection services)
        => services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly))
            .AddTransient(typeof(IPipelineBehavior<,>),typeof(ValidationPipelineBehavior<,>))
            //.AddTransient(typeof(IPipelineBehavior<,>),typeof(PerformancePipelineBehavior<,>));
            //.AddTransient(typeof(IPipelineBehavior<,>),typeof(ValidationPipelineBehavior<,>));
            .AddValidatorsFromAssembly(Contract.AssemblyReference.Assembly, includeInternalTypes: true)
            ;

        public static IServiceCollection AddConfigureAutoMapper(this IServiceCollection services)
            => services.AddAutoMapper(typeof(ServiceProfile));
        // config AutoMapper
    }
}
