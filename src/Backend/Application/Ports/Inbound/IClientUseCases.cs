using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Inbound;

public interface IAtivarPlacaOnboardingUseCase
{
    Task<Result<PlacaAtivadaDto>> ExecutarAsync(AtivarPlacaOnboardingRequest request, CancellationToken cancellationToken = default);
}

public interface IAtualizarUrlPlacaUseCase
{
    Task<Result> ExecutarAsync(Guid empresaId, AtualizarUrlPlacaRequest request, CancellationToken cancellationToken = default);
}

public interface IConfigurarFiltroSatisfacaoUseCase
{
    Task<Result> ExecutarAsync(Guid empresaId, ConfigurarFiltroSatisfacaoRequest request, CancellationToken cancellationToken = default);
}

public interface IListarPlacasClienteUseCase
{
    Task<Result<IReadOnlyList<PlacaClienteDto>>> ExecutarAsync(Guid empresaId, CancellationToken cancellationToken = default);
}

public interface IObterAnalyticsClienteUseCase
{
    Task<Result<AnalyticsClienteDto>> ExecutarAsync(Guid empresaId, Guid? placaId, int dias, CancellationToken cancellationToken = default);
}
