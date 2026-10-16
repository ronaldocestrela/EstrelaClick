using EstrelaClick.Shared.SharedKernel.Admin;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Inbound;

public interface IObterMetricasFabricaUseCase
{
    Task<Result<MetricasFabricaDto>> ExecutarAsync(CancellationToken cancellationToken = default);
}

public interface IListarLotesAdminUseCase
{
    Task<Result<IReadOnlyList<LoteItemDto>>> ExecutarAsync(CancellationToken cancellationToken = default);
}

public interface IConsultarAuditoriaPlacaUseCase
{
    Task<Result<PlacaAuditoriaDto>> ExecutarAsync(string termoBusca, CancellationToken cancellationToken = default);
}

public interface IListarEmpresasAdminUseCase
{
    Task<Result<IReadOnlyList<EmpresaDto>>> ExecutarAsync(CancellationToken cancellationToken = default);
}

public interface ICadastrarEmpresaAdminUseCase
{
    Task<Result<EmpresaDto>> ExecutarAsync(CriarEmpresaRequest request, CancellationToken cancellationToken = default);
}
