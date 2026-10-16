using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.UseCases;

public sealed class GerarLotePlacasUseCase : IGerarLotePlacasUseCase
{
    private readonly IPlacaRepository _placaRepository;
    private readonly ISlugGenerator _slugGenerator;

    public GerarLotePlacasUseCase(IPlacaRepository placaRepository, ISlugGenerator slugGenerator)
    {
        _placaRepository = placaRepository;
        _slugGenerator = slugGenerator;
    }

    public async Task<Result<LotePlacasCriadoDto>> ExecutarAsync(GerarLotePlacasCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.NumeroLote) || command.NumeroLote.Trim().Length > 32)
        {
            return Result<LotePlacasCriadoDto>.Failure(PlacaErrors.LoteInvalido);
        }

        if (command.Quantidade < 1 || command.Quantidade > 10_000)
        {
            return Result<LotePlacasCriadoDto>.Failure(PlacaErrors.QuantidadeInvalida);
        }

        var loteNormalizado = command.NumeroLote.Trim();

        var loteExiste = await _placaRepository.ExisteLoteAsync(loteNormalizado, cancellationToken);
        if (loteExiste)
        {
            return Result<LotePlacasCriadoDto>.Failure(PlacaErrors.LoteJaExiste);
        }

        var slugs = new List<string>(command.Quantidade);
        var placas = new List<Placa>(command.Quantidade);

        for (int i = 0; i < command.Quantidade; i++)
        {
            var slug = _slugGenerator.GerarSlug();
            var placaResult = Placa.Criar(slug, loteNormalizado);

            if (placaResult.IsFailure || placaResult.Value is null)
            {
                return Result<LotePlacasCriadoDto>.Failure(placaResult.Error);
            }

            slugs.Add(slug);
            placas.Add(placaResult.Value);
        }

        await _placaRepository.InserirLoteAsync(placas, cancellationToken);

        var dto = new LotePlacasCriadoDto(
            loteNormalizado,
            placas.Count,
            DateTime.UtcNow,
            slugs);

        return Result<LotePlacasCriadoDto>.Success(dto);
    }
}
