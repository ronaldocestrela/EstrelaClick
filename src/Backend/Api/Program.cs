using EstrelaClick.Backend.Api.Endpoints;
using EstrelaClick.Backend.Api.Logging;
using EstrelaClick.Backend.Application;
using EstrelaClick.Backend.Infrastructure;
using EstrelaClick.Backend.Infrastructure.Persistence;
using EstrelaClick.Shared.SharedKernel.Configuration;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

// Carrega variáveis de ambiente do arquivo .env (se presente)
DotEnv.Load();

// Inicializa o bootstrap logger para capturar logs pré-arranque
SerilogConfigurationExtensions.CreateBootstrapLogger();

try
{
    Log.Information("Iniciando EstrelaClick Backend API...");

    var builder = WebApplication.CreateBuilder(args);

    // Registra o Serilog no Host da aplicação
    builder.AddSerilogLogging();

    // Configuração do OpenAPI Nativo com Scalar
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "EstrelaClick Backend API";
            document.Info.Version = "v1";
            document.Info.Description = "API de Redirecionamento, Geração de QR Code e Esteira Fabril Mobile NFC.";

            var apiKeyScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = "X-Api-Key",
                In = ParameterLocation.Header,
                Description = "Chave de API interna da fábrica"
            };

            var jwtScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token JWT para operadores de bancada"
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["ApiKey"] = apiKeyScheme;
            document.Components.SecuritySchemes["Bearer"] = jwtScheme;

            return Task.CompletedTask;
        });
    });

    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    // Middleware de Request Logging estruturado do Serilog
    app.UseSerilogRequestLoggingMiddleware();

    // Executa as migrações do EF Core no arranque da aplicação
    await app.ApplyDatabaseMigrationsAsync();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("EstrelaClick API Docs")
                   .WithTheme(ScalarTheme.Moon);
        });
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    // Mapeamento de Endpoints
    app.MapRedirecionamentoEndpoints();
    app.MapQrCodeEndpoints();
    app.MapPlacasFabricaEndpoints();
    app.MapAuthEndpoints();
    app.MapAdminEndpoints();
    app.MapClientEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Falha fatal durante o arranque da EstrelaClick Backend API.");
    throw;
}
finally
{
    Log.Information("Encerrando EstrelaClick Backend API.");
    Log.CloseAndFlush();
}

public partial class Program;
