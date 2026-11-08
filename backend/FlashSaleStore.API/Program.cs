using Carter;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using FlashSaleStore.API.Middleware;
using FlashSaleStore.API.DependencyInjection.Extensions;
using FlashSaleStore.Application.DependencyInjection.Extensions;
using FlashSaleStore.Persistence.DependencyInjection.Extensions;
using FlashSaleStore.Presentation.APIs.Categories;
using FlashSaleStore.Presentation.APIs.Products;
using FlashSaleStore.Presentation.APIs;
using FlashSaleStore.API.BackgroundServices;
using FlashSaleStore.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

Log.Logger = new LoggerConfiguration().ReadFrom
    .Configuration(builder.Configuration).CreateLogger();

builder.Logging.ClearProviders().AddSerilog();

builder.Host.UseSerilog();

builder.Services.AddConfigureMediatR();
builder.Services.AddConfigureAutoMapper();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OrderExpiryBackgroundService>();
var jwtSigningKey = builder.Configuration["Authentication:Jwt:SigningKey"];
var jwtIssuer = builder.Configuration["Authentication:Jwt:Issuer"];
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"];
var jwtKeyBytes = string.IsNullOrWhiteSpace(jwtSigningKey) ? RandomNumberGenerator.GetBytes(32) : Encoding.UTF8.GetBytes(jwtSigningKey);
if (string.IsNullOrWhiteSpace(jwtSigningKey))
    Log.Warning("Authentication:Jwt:SigningKey is not configured; bearer-protected endpoints will reject tokens until a signing key is supplied.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer),
            ValidIssuer = jwtIssuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StoreOwner", policy => policy.RequireAuthenticatedUser().RequireRole("StoreOwner"));
});

// Add Persistence (Raw SQL / Dapper / SqlConnectionFactory)
builder.Services.AddPersistence(builder.Configuration);

//add middleware 
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

// add carter + map carter module
builder.Services.AddCarter(configurator: configurator =>
{
    configurator.WithModule<CategoryApi>();
    configurator.WithModule<ProductApi>();
    configurator.WithModule<FlashSaleAdminApi>();
    configurator.WithModule<CartApi>();
    configurator.WithModule<OrderApi>();
    configurator.WithModule<PaymentApi>();
});
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
app.UseAuthentication();
app.UseAuthorization();
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
