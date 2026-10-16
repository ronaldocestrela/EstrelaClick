using EstrelaClick.Shared.SharedKernel.Enums;

namespace EstrelaClick.Backend.Application.DTOs;

public sealed record AtivarPlacaOnboardingRequest(
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

public sealed record AtualizarUrlPlacaRequest(
    Guid PlacaId,
    string NovaUrlDestino);

public sealed record ConfigurarFiltroSatisfacaoRequest(
    Guid PlacaId,
    bool Ativo,
    string? UrlSacInterno);

public sealed record PlacaClienteDto(
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

public sealed record AnalyticsClienteDto(
    long TotalToquesPeriodo,
    long TotalToquesHoje,
    double MediaDiariaToques,
    IReadOnlyList<ToquesDiaDto> ToquesPorDia,
    IReadOnlyList<ToquesHoraDto> ToquesPorHora,
    IReadOnlyList<DispositivoContagemDto> Dispositivos,
    IReadOnlyList<UltimoAcessoDto> UltimosAcessos);
