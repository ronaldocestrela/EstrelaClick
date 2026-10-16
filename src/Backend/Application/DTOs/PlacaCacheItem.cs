using EstrelaClick.Shared.SharedKernel.Enums;

namespace EstrelaClick.Backend.Application.DTOs;

public sealed record PlacaCacheItem(
    Guid PlacaId,
    string Slug,
    StatusPlaca StatusPlaca,
    string? UrlDestino,
    bool FiltroSatisfacaoAtivo = false,
    string? UrlSacInterno = null);
