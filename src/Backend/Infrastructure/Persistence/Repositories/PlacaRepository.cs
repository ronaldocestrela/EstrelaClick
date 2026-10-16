using System.Data;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace EstrelaClick.Backend.Infrastructure.Persistence.Repositories;

public sealed class PlacaRepository : IPlacaRepository
{
    private readonly EstrelaClickDbContext _dbContext;

    public PlacaRepository(EstrelaClickDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Placa>> ObterPorLoteAsync(string numeroLote, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Placas
            .AsNoTracking()
            .Where(p => p.NumeroLote == numeroLote)
            .OrderBy(p => p.DataCriacaoUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Placa?> ObterPorSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Placas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<bool> ExisteLoteAsync(string numeroLote, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Placas
            .AsNoTracking()
            .AnyAsync(p => p.NumeroLote == numeroLote, cancellationToken);
    }

    public async Task InserirLoteAsync(IEnumerable<Placa> placas, CancellationToken cancellationToken = default)
    {
        await _dbContext.Placas.AddRangeAsync(placas, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Placa?> ObterProximaPendenteComReservaAsync(
        string? numeroLote,
        TimeSpan ttlReserva,
        string? operadorId,
        CancellationToken cancellationToken = default)
    {
        var momentoUtc = DateTime.UtcNow;
        var limiteExpiracao = momentoUtc.Subtract(ttlReserva);

        if (_dbContext.Database.IsRelational())
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

                var query = _dbContext.Placas
                    .Where(p => (numeroLote == null || p.NumeroLote == numeroLote) &&
                                (p.StatusPlaca == StatusPlaca.Criada ||
                                 (p.StatusPlaca == StatusPlaca.EmGravacao && p.DataReservaUtc != null && p.DataReservaUtc < limiteExpiracao)))
                    .OrderBy(p => p.DataCriacaoUtc);

                var placa = await query.FirstOrDefaultAsync(cancellationToken);

                if (placa is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                placa.ReservarParaGravacao(ttlReserva, operadorId, momentoUtc);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return placa;
            });
        }

        // Provedor não-relacional (em testes de memória)
        var queryMemoria = _dbContext.Placas
            .Where(p => (numeroLote == null || p.NumeroLote == numeroLote) &&
                        (p.StatusPlaca == StatusPlaca.Criada ||
                         (p.StatusPlaca == StatusPlaca.EmGravacao && p.DataReservaUtc != null && p.DataReservaUtc < limiteExpiracao)))
            .OrderBy(p => p.DataCriacaoUtc);

        var placaMemoria = await queryMemoria.FirstOrDefaultAsync(cancellationToken);
        if (placaMemoria is null)
        {
            return null;
        }

        placaMemoria.ReservarParaGravacao(ttlReserva, operadorId, momentoUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return placaMemoria;
    }

    public async Task<Placa?> ObterPorSlugParaAtualizacaoAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Placas
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task AtualizarAsync(Placa placa, CancellationToken cancellationToken = default)
    {
        _dbContext.Placas.Update(placa);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EstrelaClick.Shared.SharedKernel.Admin.MetricasFabricaDto> ObterMetricasFabricaAsync(CancellationToken cancellationToken = default)
    {
        var total = await _dbContext.Placas.AsNoTracking().CountAsync(cancellationToken);
        var criadas = await _dbContext.Placas.AsNoTracking().CountAsync(p => p.StatusPlaca == StatusPlaca.Criada, cancellationToken);
        var emGravacao = await _dbContext.Placas.AsNoTracking().CountAsync(p => p.StatusPlaca == StatusPlaca.EmGravacao, cancellationToken);
        var gravadas = await _dbContext.Placas.AsNoTracking().CountAsync(p => p.StatusPlaca == StatusPlaca.Gravada, cancellationToken);
        var vinculadas = await _dbContext.Placas.AsNoTracking().CountAsync(p => p.StatusPlaca == StatusPlaca.Vinculada, cancellationToken);
        var totalLotes = await _dbContext.Placas.AsNoTracking().Select(p => p.NumeroLote).Distinct().CountAsync(cancellationToken);

        return new EstrelaClick.Shared.SharedKernel.Admin.MetricasFabricaDto(
            TotalPlacas: total,
            TotalCriadas: criadas,
            TotalEmGravacao: emGravacao,
            TotalGravadas: gravadas,
            TotalVinculadas: vinculadas,
            TotalLotes: totalLotes
        );
    }

    public async Task<IReadOnlyList<EstrelaClick.Shared.SharedKernel.Admin.LoteItemDto>> ListarResumoLotesAsync(CancellationToken cancellationToken = default)
    {
        var agrupados = await _dbContext.Placas
            .AsNoTracking()
            .GroupBy(p => p.NumeroLote)
            .Select(g => new
            {
                NumeroLote = g.Key,
                TotalPlacas = g.Count(),
                QuantidadeCriada = g.Count(p => p.StatusPlaca == StatusPlaca.Criada),
                QuantidadeEmGravacao = g.Count(p => p.StatusPlaca == StatusPlaca.EmGravacao),
                QuantidadeGravada = g.Count(p => p.StatusPlaca == StatusPlaca.Gravada),
                QuantidadeVinculada = g.Count(p => p.StatusPlaca == StatusPlaca.Vinculada),
                DataCriacaoUtc = g.Min(p => p.DataCriacaoUtc)
            })
            .OrderByDescending(l => l.DataCriacaoUtc)
            .ToListAsync(cancellationToken);

        return agrupados.Select(l => new EstrelaClick.Shared.SharedKernel.Admin.LoteItemDto(
            l.NumeroLote,
            l.TotalPlacas,
            l.QuantidadeCriada,
            l.QuantidadeEmGravacao,
            l.QuantidadeGravada,
            l.QuantidadeVinculada,
            l.DataCriacaoUtc
        )).ToList();
    }

    public async Task<Placa?> ObterPorSlugOuUidAsync(string termo, CancellationToken cancellationToken = default)
    {
        var termoNormalizado = termo.Trim();
        return await _dbContext.Placas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == termoNormalizado || (p.ChipUid != null && p.ChipUid == termoNormalizado), cancellationToken);
    }

    public async Task<int> ObterTotalAcessosPlacaAsync(Guid placaId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AcessosMetricas
            .AsNoTracking()
            .CountAsync(a => a.PlacaId == placaId, cancellationToken);
    }

    public async Task<IReadOnlyList<Placa>> ObterPorEmpresaIdAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Placas
            .AsNoTracking()
            .Where(p => p.EmpresaId == empresaId)
            .OrderByDescending(p => p.DataAtivacaoUtc ?? p.DataCriacaoUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Placa?> ObterPorIdAsync(Guid placaId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Placas
            .FirstOrDefaultAsync(p => p.Id == placaId, cancellationToken);
    }

    public async Task<EstrelaClick.Backend.Application.DTOs.AnalyticsClienteDto> ObterAnalyticsEmpresaAsync(
        Guid empresaId,
        Guid? placaId,
        int dias,
        CancellationToken cancellationToken = default)
    {
        var placasQuery = _dbContext.Placas.AsNoTracking().Where(p => p.EmpresaId == empresaId);
        if (placaId.HasValue && placaId.Value != Guid.Empty)
        {
            placasQuery = placasQuery.Where(p => p.Id == placaId.Value);
        }

        var placasIds = await placasQuery.Select(p => p.Id).ToListAsync(cancellationToken);
        if (placasIds.Count == 0)
        {
            return new EstrelaClick.Backend.Application.DTOs.AnalyticsClienteDto(
                TotalToquesPeriodo: 0,
                TotalToquesHoje: 0,
                MediaDiariaToques: 0,
                ToquesPorDia: Array.Empty<EstrelaClick.Backend.Application.DTOs.ToquesDiaDto>(),
                ToquesPorHora: Array.Empty<EstrelaClick.Backend.Application.DTOs.ToquesHoraDto>(),
                Dispositivos: Array.Empty<EstrelaClick.Backend.Application.DTOs.DispositivoContagemDto>(),
                UltimosAcessos: Array.Empty<EstrelaClick.Backend.Application.DTOs.UltimoAcessoDto>()
            );
        }

        var hojeUtc = DateTime.UtcNow.Date;
        var dataCorteUtc = hojeUtc.AddDays(-dias);

        var acessos = await _dbContext.AcessosMetricas
            .AsNoTracking()
            .Where(a => placasIds.Contains(a.PlacaId) && a.DataHoraUtc >= dataCorteUtc)
            .OrderByDescending(a => a.DataHoraUtc)
            .ToListAsync(cancellationToken);

        var totalPeriodo = acessos.Count;
        var totalHoje = acessos.Count(a => a.DataHoraUtc >= hojeUtc);
        var mediaDiaria = dias > 0 ? Math.Round((double)totalPeriodo / dias, 1) : 0;

        var toquesPorDia = acessos
            .GroupBy(a => DateOnly.FromDateTime(a.DataHoraUtc.Date))
            .OrderBy(g => g.Key)
            .Select(g => new EstrelaClick.Backend.Application.DTOs.ToquesDiaDto(g.Key, g.Count()))
            .ToList();

        var toquesPorHora = acessos
            .GroupBy(a => a.DataHoraUtc.Hour)
            .OrderBy(g => g.Key)
            .Select(g => new EstrelaClick.Backend.Application.DTOs.ToquesHoraDto(g.Key, g.Count()))
            .ToList();

        var totalDispositivos = Math.Max(totalPeriodo, 1);
        var gruposDispositivos = acessos
            .GroupBy(a => CategorizarUserAgent(a.UserAgent))
            .Select(g => new EstrelaClick.Backend.Application.DTOs.DispositivoContagemDto(
                Categoria: g.Key,
                Total: g.Count(),
                Percentual: Math.Round((double)g.Count() * 100.0 / totalDispositivos, 1)
            ))
            .OrderByDescending(d => d.Total)
            .ToList();

        var ultimos = acessos
            .Take(15)
            .Select(a => new EstrelaClick.Backend.Application.DTOs.UltimoAcessoDto(
                DataHoraUtc: a.DataHoraUtc,
                Dispositivo: CategorizarUserAgent(a.UserAgent),
                OrigemReferer: a.OrigemReferer
            ))
            .ToList();

        return new EstrelaClick.Backend.Application.DTOs.AnalyticsClienteDto(
            TotalToquesPeriodo: totalPeriodo,
            TotalToquesHoje: totalHoje,
            MediaDiariaToques: mediaDiaria,
            ToquesPorDia: toquesPorDia,
            ToquesPorHora: toquesPorHora,
            Dispositivos: gruposDispositivos,
            UltimosAcessos: ultimos
        );
    }

    private static string CategorizarUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "Outro / Desconhecido";
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("ios")) return "iOS (Apple)";
        if (ua.Contains("windows") || ua.Contains("macintosh") || ua.Contains("linux")) return "Desktop";
        return "Mobile / Outro";
    }
}
