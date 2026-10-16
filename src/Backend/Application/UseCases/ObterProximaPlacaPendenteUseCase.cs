using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Results;
using Microsoft.Extensions.Configuration;

namespace EstrelaClick.Backend.Application.UseCases;

public sealed class ObterProximaPlacaPendenteUseCase : IObterProximaPlacaPendenteUseCase
{
    private static readonly TimeSpan DefaultTtlReserva = TimeSpan.FromSeconds(90);

    private readonly IPlacaRepository _placaRepository;
    private readonly string _baseRedirectUrl;

    public ObterProximaPlacaPendenteUseCase(IPlacaRepository placaRepository, IConfiguration configuration)
    {
        _placaRepository = placaRepository;
        _baseRedirectUrl = (configuration["App:BaseRedirectUrl"] ?? "https://redirect.estrelaclick.com.br").TrimEnd('/');
    }

    public async Task<Result<PlacaPendenteDto>> ExecutarAsync(ObterProximaPlacaPendenteQuery query, CancellationToken cancellationToken = default)
    {
        var numeroLote = string.IsNullOrWhiteSpace(query.NumeroLote) ? null : query.NumeroLote.Trim();
        var operadorId = string.IsNullOrWhiteSpace(query.OperadorId) ? null : query.OperadorId.Trim();

        var placa = await _placaRepository.ObterProximaPendenteComReservaAsync(
            numeroLote,
            DefaultTtlReserva,
            operadorId,
            cancellationToken);

        if (placa is null)
        {
            return Result<PlacaPendenteDto>.Failure(PlacaErrors.FilaVazia);
        }

        var dataReserva = placa.DataReservaUtc ?? DateTime.UtcNow;
        var dataExpiracao = dataReserva.Add(DefaultTtlReserva);
        var urlRedirecionamento = $"{_baseRedirectUrl}/r/{placa.Slug}";

        var dto = new PlacaPendenteDto(
            placa.Id,
            placa.Slug,
            placa.NumeroLote,
            urlRedirecionamento,
            dataReserva,
            dataExpiracao,
            placa.OperadorReservaId);

        return Result<PlacaPendenteDto>.Success(dto);
    }
}
