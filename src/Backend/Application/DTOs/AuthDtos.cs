namespace EstrelaClick.Backend.Application.DTOs;

public record AutenticarOperadorCommand(string Login, string Senha);

public record AuthTokenResult(
    string Token,
    string OperadorId,
    string Nome,
    DateTime ExpiracaoUtc);

public record LoteResumoDto(
    string NumeroLote,
    int TotalPlacas,
    int TotalGravadas,
    int TotalPendentes,
    int TotalVinculadas);
