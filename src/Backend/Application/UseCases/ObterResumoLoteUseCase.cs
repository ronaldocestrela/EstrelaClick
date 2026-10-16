using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Enums;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.UseCases;

public sealed class ObterResumoLoteUseCase : IObterResumoLoteUseCase
{
    private readonly IPlacaRepository _placaRepository;

    public ObterResumoLoteUseCase(IPlacaRepository placaRepository)
    {
        _placaRepository = placaRepository;
    }

    public async Task<Result<LoteResumoDto>> ExecutarAsync(string numeroLote, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numeroLote))
        {
            return Result<LoteResumoDto>.Failure(PlacaErrors.LoteInvalido);
        }

        var loteSanitizado = numeroLote.Trim();
        var placas = await _placaRepository.ObterPorLoteAsync(loteSanitizado, cancellationToken);

        if (placas.Count == 0)
        {
            return Result<LoteResumoDto>.Failure(PlacaErrors.LoteNaoEncontrado);
        }

        var total = placas.Count;
        var gravadas = placas.Count(p => p.StatusPlaca == StatusPlaca.Gravada);
        var pendentes = placas.Count(p => p.StatusPlaca == StatusPlaca.Criada || p.StatusPlaca == StatusPlaca.EmGravacao);
        var vinculadas = placas.Count(p => p.StatusPlaca == StatusPlaca.Vinculada);

        return Result<LoteResumoDto>.Success(new LoteResumoDto(
            NumeroLote: loteSanitizado,
            TotalPlacas: total,
            TotalGravadas: gravadas,
            TotalPendentes: pendentes,
            TotalVinculadas: vinculadas));
    }
}
