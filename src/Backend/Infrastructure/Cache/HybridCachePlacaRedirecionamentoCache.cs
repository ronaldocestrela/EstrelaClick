using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace EstrelaClick.Backend.Infrastructure.Cache;

public sealed class HybridCachePlacaRedirecionamentoCache : IPlacaRedirecionamentoCache
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(30),
        LocalCacheExpiration = TimeSpan.FromMinutes(30)
    };

    private readonly HybridCache _hybridCache;
    private readonly IServiceScopeFactory _scopeFactory;

    public HybridCachePlacaRedirecionamentoCache(
        HybridCache hybridCache,
        IServiceScopeFactory scopeFactory)
    {
        _hybridCache = hybridCache;
        _scopeFactory = scopeFactory;
    }

    public async ValueTask<PlacaCacheItem?> ObterPorSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"slug:{slug.ToLowerInvariant()}";

        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();

                var placa = await dbContext.Placas
                    .AsNoTracking()
                    .Where(p => p.Slug == slug)
                    .Select(p => new PlacaCacheItem(p.Id, p.Slug, p.StatusPlaca, p.UrlDestino, p.FiltroSatisfacaoAtivo, p.UrlSacInterno))
                    .FirstOrDefaultAsync(token);

                return placa;
            },
            CacheOptions,
            cancellationToken: cancellationToken);
    }

    public async ValueTask RemoverAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"slug:{slug.ToLowerInvariant()}";
        await _hybridCache.RemoveAsync(cacheKey, cancellationToken);
    }
}
