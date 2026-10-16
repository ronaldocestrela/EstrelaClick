using EstrelaClick.Backend.Api.Endpoints;
using EstrelaClick.Backend.Application;
using EstrelaClick.Backend.Infrastructure;
using EstrelaClick.Backend.Infrastructure.Persistence;
using EstrelaClick.Shared.SharedKernel.Configuration;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

// Carrega variáveis de ambiente do arquivo .env (se presente)
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

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

app.UseHttpsRedirection();

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

public partial class Program;
