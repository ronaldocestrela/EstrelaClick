using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace EstrelaClick.Backend.Api.Logging;

/// <summary>
/// Extensões para padronização e configuração do Serilog no ecossistema EstrelaClick.
/// </summary>
public static class SerilogConfigurationExtensions
{
    private const string DefaultConsoleTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    private const string DefaultFileTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{Application}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Inicializa o Bootstrap Logger do Serilog para capturar falhas no arranque prematuro da aplicação.
    /// </summary>
    public static void CreateBootstrapLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "EstrelaClick.Backend.Api (Bootstrap)")
            .WriteTo.Console(outputTemplate: DefaultConsoleTemplate)
            .CreateBootstrapLogger();
    }

    /// <summary>
    /// Constrói uma instância de <see cref="LoggerConfiguration"/> pré-configurada com enrichers e sinks padrão.
    /// </summary>
    public static LoggerConfiguration CreateLoggerConfiguration(IConfiguration configuration, IHostEnvironment environment)
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("Scalar.AspNetCore", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "EstrelaClick.Backend.Api")
            .Enrich.WithProperty("Environment", environment.EnvironmentName)
            .ReadFrom.Configuration(configuration);

        // Se sinks não foram explicitamente declarados no IConfiguration, garante os sinks padrão (Console + Arquivo Rotativo)
        var hasConfiguredSinks = configuration.GetSection("Serilog:WriteTo").GetChildren().Any();
        if (!hasConfiguredSinks)
        {
            loggerConfig
                .WriteTo.Console(outputTemplate: DefaultConsoleTemplate)
                .WriteTo.File(
                    path: "logs/estrelaclick-api-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    outputTemplate: DefaultFileTemplate);
        }

        return loggerConfig;
    }

    /// <summary>
    /// Configura o Serilog como provedor de log padrão no host da Web API.
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            var config = CreateLoggerConfiguration(context.Configuration, context.HostingEnvironment);

            // Transfere as configurações construídas
            loggerConfiguration.MinimumLevel.Information()
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "EstrelaClick.Backend.Api")
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

            var hasConfiguredSinks = context.Configuration.GetSection("Serilog:WriteTo").GetChildren().Any();
            if (!hasConfiguredSinks)
            {
                loggerConfiguration
                    .WriteTo.Console(outputTemplate: DefaultConsoleTemplate)
                    .WriteTo.File(
                        path: "logs/estrelaclick-api-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        fileSizeLimitBytes: 10 * 1024 * 1024,
                        rollOnFileSizeLimit: true,
                        outputTemplate: DefaultFileTemplate);
            }
        });

        return builder;
    }

    /// <summary>
    /// Habilita o middleware de requisições estruturadas do Serilog (`UseSerilogRequestLogging`).
    /// </summary>
    public static WebApplication UseSerilogRequestLoggingMiddleware(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = EnrichDiagnosticContext;
        });

        return app;
    }

    /// <summary>
    /// Enriquece o contexto diagnóstico do Serilog com propriedades chave da requisição HTTP.
    /// </summary>
    public static void EnrichDiagnosticContext(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        var request = httpContext.Request;

        diagnosticContext.Set("RequestHost", request.Host.Value);
        diagnosticContext.Set("RequestScheme", request.Scheme);
        diagnosticContext.Set("TraceIdentifier", httpContext.TraceIdentifier);

        if (request.Headers.TryGetValue("User-Agent", out var userAgent))
        {
            diagnosticContext.Set("UserAgent", userAgent.ToString());
        }

        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(clientIp))
        {
            diagnosticContext.Set("ClientIp", clientIp);
        }

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? httpContext.User.Identity?.Name;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            diagnosticContext.Set("UserId", userId);
        }
    }
}
