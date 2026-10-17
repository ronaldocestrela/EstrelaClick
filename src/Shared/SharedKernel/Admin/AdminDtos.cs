using EstrelaClick.Shared.SharedKernel.Enums;

namespace EstrelaClick.Shared.SharedKernel.Admin;

public sealed record MetricasFabricaDto(
    int TotalPlacas,
    int TotalCriadas,
    int TotalEmGravacao,
    int TotalGravadas,
    int TotalVinculadas,
    int TotalLotes
);

public sealed record LoteItemDto(
    string NumeroLote,
    int TotalPlacas,
    int QuantidadeCriada,
    int QuantidadeEmGravacao,
    int QuantidadeGravada,
    int QuantidadeVinculada,
    DateTime DataCriacaoUtc
);

public sealed record PlacaAuditoriaDto(
    Guid Id,
    string Slug,
    string NumeroLote,
    StatusPlaca Status,
    string? ChipUid,
    string? UrlDestino,
    Guid? EmpresaId,
    string? EmpresaRazaoSocial,
    string? OperadorReservaId,
    DateTime DataCriacaoUtc,
    DateTime? DataGravacaoUtc,
    DateTime? DataAtivacaoUtc,
    int TotalAcessos
);

public sealed record EmpresaDto(
    Guid Id,
    string RazaoSocial,
    string Email,
    string Telefone,
    bool StatusAtivo,
    DateTime DataCadastroUtc,
    int TotalPlacas
);

public sealed record CriarEmpresaRequest(
    string RazaoSocial,
    string Email,
    string Telefone
);

public sealed record GerarLoteRequest(
    string NumeroLote,
    int Quantidade
);

public sealed record LoteCriadoDto(
    string NumeroLote,
    int QuantidadeCriada,
    DateTime DataCriacaoUtc,
    IReadOnlyList<string> Slugs
);

public sealed record AdminLoginRequest(
    string Usuario,
    string Senha,
    bool LembrarMe = false
);

public sealed record AdminAuthSuccessDto(
    string Usuario,
    string Role,
    DateTime AutenticadoEmUtc
);
