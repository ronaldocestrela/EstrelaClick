using EstrelaClick.Backend.Domain.Entities;

namespace EstrelaClick.Backend.Application.Ports.Outbound;

public interface IPlacaRepository
{
    Task<IReadOnlyList<Placa>> ObterPorLoteAsync(string numeroLote, CancellationToken cancellationToken = default);
    Task<Placa?> ObterPorSlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExisteLoteAsync(string numeroLote, CancellationToken cancellationToken = default);
    Task InserirLoteAsync(IEnumerable<Placa> placas, CancellationToken cancellationToken = default);
    Task<Placa?> ObterProximaPendenteComReservaAsync(string? numeroLote, TimeSpan ttlReserva, string? operadorId, CancellationToken cancellationToken = default);
    Task<Placa?> ObterPorSlugParaAtualizacaoAsync(string slug, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Placa placa, CancellationToken cancellationToken = default);
    Task<EstrelaClick.Shared.SharedKernel.Admin.MetricasFabricaDto> ObterMetricasFabricaAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EstrelaClick.Shared.SharedKernel.Admin.LoteItemDto>> ListarResumoLotesAsync(CancellationToken cancellationToken = default);
    Task<Placa?> ObterPorSlugOuUidAsync(string termo, CancellationToken cancellationToken = default);
    Task<int> ObterTotalAcessosPlacaAsync(Guid placaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Placa>> ObterPorEmpresaIdAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<Placa?> ObterPorIdAsync(Guid placaId, CancellationToken cancellationToken = default);
    Task<EstrelaClick.Backend.Application.DTOs.AnalyticsClienteDto> ObterAnalyticsEmpresaAsync(Guid empresaId, Guid? placaId, int dias, CancellationToken cancellationToken = default);
}
