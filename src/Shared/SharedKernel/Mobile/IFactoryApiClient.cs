using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Shared.SharedKernel.Mobile;

public interface IFactoryApiClient
{
    Task<Result<OperadorLoginResult>> LoginAsync(string login, string senha, CancellationToken ct = default);
    Task<Result<PlacaAtivaInfo>> ObterProximaPendenteAsync(string? numeroLote = null, CancellationToken ct = default);
    Task<Result<ResumoLoteInfo>> ObterResumoLoteAsync(string numeroLote, CancellationToken ct = default);
    Task<Result<ConfirmacaoGravacaoResult>> ConfirmarGravacaoAsync(string slug, string chipUid, CancellationToken ct = default);
}
