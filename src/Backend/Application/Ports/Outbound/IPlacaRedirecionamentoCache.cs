using EstrelaClick.Backend.Application.DTOs;

namespace EstrelaClick.Backend.Application.Ports.Outbound;

public interface IPlacaRedirecionamentoCache
{
    ValueTask<PlacaCacheItem?> ObterPorSlugAsync(string slug, CancellationToken cancellationToken = default);
    ValueTask RemoverAsync(string slug, CancellationToken cancellationToken = default);
}
