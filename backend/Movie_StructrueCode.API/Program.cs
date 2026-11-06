using Carter;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Movie_StructrueCode.API.Middleware;
using Movie_StructureCode.API.DependencyInjection.Extensions;
using Movie_StructureCode.Application.DependencyInjection.Extensions;
using Movie_StructureCode.Persistence.DependencyInjection.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

Log.Logger = new LoggerConfiguration().ReadFrom
    .Configuration(builder.Configuration).CreateLogger();

builder.Logging.ClearProviders().AddSerilog();

builder.Host.UseSerilog();

builder.Services.AddConfigureMediatR();
builder.Services.AddConfigureAutoMapper();

// Add Persistence (Raw SQL / Dapper / SqlConnectionFactory)
builder.Services.AddPersistence(builder.Configuration);

//add middleware 
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

// add carter + map carter module
builder.Services.AddCarter();
builder.Services.AddEndpointsApiExplorer();

// add Swagger
builder.Services.AddSwaggerGenNewtonsoftSupport()
                .AddFluentValidationRulesToSwagger()
                .AddSwagger();

// add versioning
builder.Services.AddApiVersioning(options => options.ReportApiVersions = true)
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapCarter();

if (builder.Environment.IsDevelopment() || builder.Environment.IsStaging())
{
    app.ConfigureSwagger();
}

try
{
    await app.RunAsync();
    Log.Information("stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during bootstrapping");
    await app.StopAsync();
}
finally
{
    Log.CloseAndFlush();
    await app.DisposeAsync();
}

public partial class Programs { }
