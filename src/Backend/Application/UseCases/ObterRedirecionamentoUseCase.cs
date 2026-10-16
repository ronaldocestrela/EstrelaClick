using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Enums;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.UseCases;

public sealed class ObterRedirecionamentoUseCase : IObterRedirecionamentoUseCase
{
    private readonly IPlacaRedirecionamentoCache _cache;
    private readonly IMetricaAcessoQueue _metricaQueue;

    public ObterRedirecionamentoUseCase(
        IPlacaRedirecionamentoCache cache,
        IMetricaAcessoQueue metricaQueue)
    {
        _cache = cache;
        _metricaQueue = metricaQueue;
    }

    public async Task<Result<RedirecionamentoDto>> ExecutarAsync(
        string slug,
        string? userAgent,
        string ipHash,
        string? referer,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug) || !Guid.TryParseExact(slug, "N", out _))
        {
            return Result<RedirecionamentoDto>.Failure(PlacaErrors.SlugInvalido);
        }

        var placa = await _cache.ObterPorSlugAsync(slug, cancellationToken);
        if (placa is null || placa.StatusPlaca == StatusPlaca.Criada)
        {
            return Result<RedirecionamentoDto>.Failure(PlacaErrors.NaoEncontrada);
        }

        if (placa.StatusPlaca == StatusPlaca.Gravada)
        {
            return Result<RedirecionamentoDto>.Failure(PlacaErrors.NaoAtivada);
        }

        if (string.IsNullOrWhiteSpace(placa.UrlDestino))
        {
            return Result<RedirecionamentoDto>.Failure(PlacaErrors.NaoAtivada);
        }

        var metrica = new MetricaAcessoEvent(
            PlacaId: placa.PlacaId,
            DataHoraUtc: DateTime.UtcNow,
            UserAgent: userAgent,
            IpHash: ipHash,
            OrigemReferer: referer);

        _metricaQueue.TryEnqueue(metrica);

        return Result<RedirecionamentoDto>.Success(new RedirecionamentoDto(
            placa.PlacaId,
            placa.UrlDestino,
            placa.FiltroSatisfacaoAtivo));
    }
}
