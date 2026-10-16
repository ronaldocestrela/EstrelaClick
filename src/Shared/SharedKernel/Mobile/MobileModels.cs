namespace EstrelaClick.Shared.SharedKernel.Mobile;

public record OperadorLoginResult(
    string Token,
    string OperadorId,
    string Nome,
    DateTime ExpiracaoUtc);

public record PlacaAtivaInfo(
    Guid Id,
    string Slug,
    string NumeroLote,
    string UrlRedirecionamento,
    DateTime DataReservaUtc,
    DateTime DataExpiracaoReservaUtc,
    string? OperadorReservaId);

public record ResumoLoteInfo(
    string NumeroLote,
    int TotalPlacas,
    int TotalGravadas,
    int TotalPendentes,
    int TotalVinculadas);

public record ConfirmacaoGravacaoResult(
    Guid Id,
    string Slug,
    string ChipUid,
    DateTime DataGravacaoUtc);
