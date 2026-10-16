namespace EstrelaClick.Backend.Application.DTOs;

public record GerarLotePlacasCommand(string NumeroLote, int Quantidade);

public record LotePlacasCriadoDto(
    string NumeroLote,
    int QuantidadeCriada,
    DateTime DataCriacaoUtc,
    IReadOnlyList<string> Slugs);

public record ObterProximaPlacaPendenteQuery(string? NumeroLote = null, string? OperadorId = null);

public record PlacaPendenteDto(
    Guid Id,
    string Slug,
    string NumeroLote,
    string UrlRedirecionamento,
    DateTime DataReservaUtc,
    DateTime DataExpiracaoReservaUtc,
    string? OperadorReservaId);

public record ConfirmarGravacaoPlacaCommand(string Slug, string ChipUid, string? OperadorId = null);

public record PlacaGravadaDto(
    Guid Id,
    string Slug,
    string ChipUid,
    DateTime DataGravacaoUtc);
