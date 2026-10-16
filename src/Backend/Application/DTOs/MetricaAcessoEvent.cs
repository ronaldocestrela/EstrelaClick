namespace EstrelaClick.Backend.Application.DTOs;

public sealed record MetricaAcessoEvent(
    Guid PlacaId,
    DateTime DataHoraUtc,
    string? UserAgent,
    string IpHash,
    string? OrigemReferer);
