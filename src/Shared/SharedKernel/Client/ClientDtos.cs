using EstrelaClick.Shared.SharedKernel.Enums;

namespace EstrelaClick.Shared.SharedKernel.Client;

public sealed record VerificarOnboardingDto(
    Guid PlacaId,
    string Slug,
    string StatusPlaca,
    bool DisponivelParaAtivacao,
    string? UrlDestinoAtual,
    bool FiltroSatisfacaoAtivo,
    string? UrlSacInterno);

public sealed record AtivarPlacaRequest(
    string Slug,
    string UrlDestino,
    Guid? EmpresaExistenteId = null,
    string? RazaoSocial = null,
    string? Email = null,
    string? Telefone = null,
    string? IdentityUserId = null);

public sealed record PlacaAtivadaDto(
    Guid PlacaId,
    string Slug,
    Guid EmpresaId,
    string UrlDestino);

public sealed record AtualizarUrlRequest(
    Guid PlacaId,
    string NovaUrlDestino);

public sealed record ConfigurarFiltroRequest(
    Guid PlacaId,
    bool Ativo,
    string? UrlSacInterno);

public sealed record PlacaItemDto(
    Guid PlacaId,
    string Slug,
    string NumeroLote,
    string? UrlDestino,
    StatusPlaca StatusPlaca,
    string? ChipUid,
    DateTime DataCriacaoUtc,
    DateTime? DataAtivacaoUtc,
    bool FiltroSatisfacaoAtivo,
    string? UrlSacInterno);

public sealed record ToquesDiaDto(
    DateOnly Data,
    long TotalToques);

public sealed record ToquesHoraDto(
    int Hora,
    long TotalToques);

public sealed record DispositivoContagemDto(
    string Categoria,
    long Total,
    double Percentual);

public sealed record UltimoAcessoDto(
    DateTime DataHoraUtc,
    string Dispositivo,
    string? OrigemReferer);

public sealed record AnalyticsDto(
    long TotalToquesPeriodo,
    long TotalToquesHoje,
    double MediaDiariaToques,
    IReadOnlyList<ToquesDiaDto> ToquesPorDia,
    IReadOnlyList<ToquesHoraDto> ToquesPorHora,
    IReadOnlyList<DispositivoContagemDto> Dispositivos,
    IReadOnlyList<UltimoAcessoDto> UltimosAcessos);
