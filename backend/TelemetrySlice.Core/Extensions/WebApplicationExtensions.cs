using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TelemetrySlice.Data;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Services;

namespace TelemetrySlice.Core.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureAndBuild(this WebApplicationBuilder builder, string appName)
    {
        builder.AddAppSettings();
        
        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            var xmlFilename = $"{Assembly.GetEntryAssembly()!.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });
        
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
        var redisConnection = builder.Configuration.GetConnectionString("RedisConnection")!;
        var rabbitConnection = builder.Configuration.GetConnectionString("RabbitConnection")!;
        var rabbitHealthCheckUri = builder.Configuration.GetConnectionString("RabbitHealthCheckUri")!;
        
        builder.Services.AddDbContext<TelemetrySliceDbContext>(options =>
            options.UseSqlite(connectionString), optionsLifetime: ServiceLifetime.Scoped);
        
        var redisConfigOptions = ConfigurationOptions.Parse(redisConnection);
        redisConfigOptions.AbortOnConnectFail = false;
        redisConfigOptions.ConnectTimeout = 20000;
        redisConfigOptions.SyncTimeout = 20000;
        redisConfigOptions.ReconnectRetryPolicy = new ExponentialRetry(500, 60000);
        redisConfigOptions.ClientName = (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty) + "_General";
        redisConfigOptions.AllowAdmin = true;
        redisConfigOptions.DefaultDatabase = 0;

        var redisConnectionMulti = ConnectionMultiplexer.Connect(redisConfigOptions);
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => redisConnectionMulti);

        builder.Services.AddStackExchangeRedisCache(option =>
        {
            option.ConfigurationOptions = redisConfigOptions;
        });
        builder.Services.AddSingleton<IEventMessageService>(new EventMessageService(rabbitConnection));
        builder.Services.AddSingleton<ICacheService, CacheService>();
        
        builder.Services.AddScoped<IAdminService, AdminService>();
        builder.Services.AddScoped<ICustomerService, CustomerService>();
        builder.Services.AddScoped<IDeviceService, DeviceService>();
        builder.Services.AddScoped<IEventTelemetryService, EventTelemetryService>();
        builder.Services.AddScoped<ITelemetryService, TelemetryService>();
        
        builder.Services.AddHealthChecks()
            .AddRedis(redisConnectionMulti, name: "Redis")
            .AddRabbitMQ(new Uri(rabbitHealthCheckUri), name: "RabbitMQ");
        
        return builder.Build();
    }

    private static void AddAppSettings(this WebApplicationBuilder builder)
    {
        var env = builder.Environment;

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();
    }
}