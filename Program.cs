using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.ApplicationInsights.Extensibility;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using AzureFunctionIgga.Data;
using AzureFunctionIgga.Services;
using AzureFunctionIgga.Services.Interfaces;
using AzureFunctionIgga.Middlewares;
using AutoMapper;
using FluentValidation;
using Serilog;
using Serilog.Events;

namespace AzureFunctionIgga;

public class Program
{
    public static async Task Main()
    {
        // Configurar Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
            .CreateLogger();

        try
        {
            Log.Information("Iniciando Azure Function IGGA");

            var host = CreateHostBuilder().Build();
            
            // Aplicar migraciones de base de datos en producción
            await ApplyDatabaseMigrationsAsync(host);
            
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "La aplicación terminó inesperadamente");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return new HostBuilder()
            .ConfigureFunctionsWebApplication(builder =>
            {
                // Registrar middlewares
                builder.UseMiddleware<ExceptionHandlingMiddleware>();
                builder.UseMiddleware<AuthenticationMiddleware>();
                builder.UseMiddleware<LoggingMiddleware>();
            })
            .ConfigureServices((context, services) =>
            {
                // Configuración de Application Insights
                services.AddApplicationInsightsTelemetryWorkerService();
                services.ConfigureFunctionsApplicationInsights();

                // Configuración de Entity Framework
                var connectionString = GetConnectionString(context.Configuration);
                services.AddDbContext<IggaDbContext>(options =>
                {
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        sqlOptions.CommandTimeout(120);
                    });
                    
                    options.EnableSensitiveDataLogging(false);
                    options.EnableServiceProviderCaching();
                    options.EnableDetailedErrors();
                });

                // Configuración de AutoMapper
                services.AddAutoMapper(typeof(Program).Assembly);

                // Configuración de FluentValidation
                services.AddValidatorsFromAssembly(typeof(Program).Assembly);

                // Registro de servicios de negocio
                services.AddScoped<IUserService, UserService>();
                services.AddScoped<IDataProcessingService, DataProcessingService>();
                services.AddScoped<INotificationService, NotificationService>();
                services.AddScoped<IReportService, ReportService>();
                services.AddScoped<IAuthenticationService, AuthenticationService>();
                services.AddScoped<IFileStorageService, FileStorageService>();
                services.AddScoped<IEmailService, EmailService>();
                services.AddScoped<IAuditService, AuditService>();

                // Configuración de Azure Key Vault
                services.AddSingleton<SecretClient>(provider =>
                {
                    var keyVaultUrl = Environment.GetEnvironmentVariable("KeyVaultUrl");
                    if (!string.IsNullOrEmpty(keyVaultUrl))
                    {
                        return new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
                    }
                    return null!;
                });

                // Configuración de logging personalizada
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                });

                // Configuración de HttpClient
                services.AddHttpClient();

                // Configuración de CORS
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
                });

                // Configuración de políticas de reintentos
                services.Configure<RetryPolicyOptions>(options =>
                {
                    options.MaxRetries = 3;
                    options.DelayBetweenRetries = TimeSpan.FromSeconds(2);
                });

                // Configuración de opciones de serialización JSON
                services.ConfigureHttpJsonOptions(options =>
                {
                    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.SerializerOptions.WriteIndented = true;
                });
            })
            .UseSerilog();
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlConnectionString") 
                              ?? Environment.GetEnvironmentVariable("SqlConnectionString");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("No se encontró la cadena de conexión a la base de datos");
        }

        return connectionString;
    }

    private static async Task ApplyDatabaseMigrationsAsync(IHost host)
    {
        try
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IggaDbContext>();
            
            if (context.Database.GetPendingMigrations().Any())
            {
                Log.Information("Aplicando migraciones de base de datos...");
                await context.Database.MigrateAsync();
                Log.Information("Migraciones aplicadas exitosamente");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al aplicar migraciones de base de datos");
            throw;
        }
    }
}

public class RetryPolicyOptions
{
    public int MaxRetries { get; set; }
    public TimeSpan DelayBetweenRetries { get; set; }
}