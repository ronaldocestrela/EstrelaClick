using System.Text;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Infrastructure.Cache;
using EstrelaClick.Backend.Infrastructure.Persistence;
using EstrelaClick.Backend.Infrastructure.Persistence.Repositories;
using EstrelaClick.Backend.Infrastructure.QrCode;
using EstrelaClick.Backend.Infrastructure.Security;
using EstrelaClick.Backend.Infrastructure.Telemetry;
using EstrelaClick.Backend.Infrastructure.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EstrelaClick.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não foi configurada.");

        services.AddDbContext<EstrelaClickDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer =>
                sqlServer.MigrationsAssembly(typeof(AssemblyMarker).Assembly.FullName)));

        services.AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<EstrelaClickDbContext>();

        // Cache de dois níveis com HybridCache (.NET 10)
        services.AddHybridCache();
        services.AddSingleton<IPlacaRedirecionamentoCache, HybridCachePlacaRedirecionamentoCache>();

        // Segurança e LGPD
        services.AddSingleton<IIpHasher, IpHasher>();
        services.AddSingleton<ISlugGenerator, GuidSlugGenerator>();

        // Autenticação e Autorização Híbrida para Fábrica (ApiKey + JWT Bearer)
        var jwtSecret = configuration["Jwt:SecretKey"] ?? "chave-super-secreta-padrao-jwt-bancada-2026-estrelaclick";
        var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = ApiKeyAuthenticationOptions.DefaultScheme;
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.DefaultScheme, _ => { })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = jwtKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("FactoryAuthPolicy", policy =>
            {
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme, JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            });
        });

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IOperadorAuthenticationService, IdentityOperadorAuthenticationService>();

        // Barramento assíncrono não-bloqueante de métricas
        services.AddSingleton<ChannelMetricaAcessoQueue>();
        services.AddSingleton<IMetricaAcessoQueue>(sp => sp.GetRequiredService<ChannelMetricaAcessoQueue>());

        // Geração de QR Code e Arquivos Zip
        services.AddSingleton<IQrCodeGenerator, QRCoderGenerator>();
        services.AddSingleton<IZipArchiveService, ZipLoteArchiveService>();

        // Repositórios
        services.AddScoped<IPlacaRepository, PlacaRepository>();
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();

        // Worker em segundo plano para persistência em lote
        services.AddHostedService<MetricasBatchProcessorWorker>();

        return services;
    }
}
