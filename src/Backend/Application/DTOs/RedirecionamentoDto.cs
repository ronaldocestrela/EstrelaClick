namespace EstrelaClick.Backend.Application.DTOs;

public sealed record RedirecionamentoDto(
    Guid PlacaId,
    string UrlDestino,
    bool FiltroSatisfacaoAtivo = false);
