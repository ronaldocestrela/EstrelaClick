using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.UseCases;

public sealed class ConfirmarGravacaoPlacaUseCase : IConfirmarGravacaoPlacaUseCase
{
    private readonly IPlacaRepository _placaRepository;

    public ConfirmarGravacaoPlacaUseCase(IPlacaRepository placaRepository)
    {
        _placaRepository = placaRepository;
    }

    public async Task<Result<PlacaGravadaDto>> ExecutarAsync(ConfirmarGravacaoPlacaCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Slug) || !Guid.TryParseExact(command.Slug, "N", out _))
        {
            return Result<PlacaGravadaDto>.Failure(PlacaErrors.SlugInvalido);
        }

        if (string.IsNullOrWhiteSpace(command.ChipUid))
        {
            return Result<PlacaGravadaDto>.Failure(PlacaErrors.ChipUidInvalido);
        }

        var slugNormalizado = command.Slug.Trim().ToLowerInvariant();
        var placa = await _placaRepository.ObterPorSlugParaAtualizacaoAsync(slugNormalizado, cancellationToken);

        if (placa is null)
        {
            return Result<PlacaGravadaDto>.Failure(PlacaErrors.NaoEncontrada);
        }

        var gravacaoResult = placa.RegistrarGravacao(command.ChipUid);
        if (gravacaoResult.IsFailure)
        {
            return Result<PlacaGravadaDto>.Failure(gravacaoResult.Error);
        }

        await _placaRepository.AtualizarAsync(placa, cancellationToken);

        var dto = new PlacaGravadaDto(
            placa.Id,
            placa.Slug,
            placa.ChipUid!,
            placa.DataGravacaoUtc ?? DateTime.UtcNow);

        return Result<PlacaGravadaDto>.Success(dto);
    }
}
