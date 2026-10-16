using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Enums;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.UseCases;

public sealed class AtivarPlacaOnboardingUseCase : IAtivarPlacaOnboardingUseCase
{
    private readonly IPlacaRepository _placaRepository;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IPlacaRedirecionamentoCache _cache;

    public AtivarPlacaOnboardingUseCase(
        IPlacaRepository placaRepository,
        IEmpresaRepository empresaRepository,
        IPlacaRedirecionamentoCache cache)
    {
        _placaRepository = placaRepository;
        _empresaRepository = empresaRepository;
        _cache = cache;
    }

    public async Task<Result<PlacaAtivadaDto>> ExecutarAsync(
        AtivarPlacaOnboardingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Slug) || !Guid.TryParseExact(request.Slug, "N", out _))
        {
            return Result<PlacaAtivadaDto>.Failure(new Error("placa.slug.invalido", "Slug informado é inválido."));
        }

        var placa = await _placaRepository.ObterPorSlugParaAtualizacaoAsync(request.Slug, cancellationToken);
        if (placa is null)
        {
            return Result<PlacaAtivadaDto>.Failure(new Error("placa.naoEncontrada", "Placa não encontrada."));
        }

        if (placa.StatusPlaca != StatusPlaca.Gravada)
        {
            return Result<PlacaAtivadaDto>.Failure(new Error("placa.onboarding.statusInvalido", "Apenas placas gravadas e ainda não vinculadas podem ser ativadas no onboarding."));
        }

        Guid empresaId;

        if (request.EmpresaExistenteId.HasValue && request.EmpresaExistenteId.Value != Guid.Empty)
        {
            var empresaExistente = await _empresaRepository.ObterPorIdAsync(request.EmpresaExistenteId.Value, cancellationToken);
            if (empresaExistente is null)
            {
                return Result<PlacaAtivadaDto>.Failure(new Error("empresa.naoEncontrada", "Empresa informada não foi localizada."));
            }

            empresaId = empresaExistente.Id;
        }
        else
        {
            var criarEmpresaResult = Empresa.Criar(
                razaoSocial: request.RazaoSocial ?? string.Empty,
                email: request.Email ?? string.Empty,
                telefone: request.Telefone ?? string.Empty,
                identityUserId: request.IdentityUserId);

            if (criarEmpresaResult.IsFailure || criarEmpresaResult.Value is null)
            {
                return Result<PlacaAtivadaDto>.Failure(criarEmpresaResult.Error);
            }

            var novaEmpresa = criarEmpresaResult.Value;
            await _empresaRepository.InserirAsync(novaEmpresa, cancellationToken);
            empresaId = novaEmpresa.Id;
        }

        var vincularResult = placa.VincularEmpresa(empresaId, request.UrlDestino);
        if (vincularResult.IsFailure)
        {
            return Result<PlacaAtivadaDto>.Failure(vincularResult.Error);
        }

        await _placaRepository.AtualizarAsync(placa, cancellationToken);
        await _cache.RemoverAsync(placa.Slug, cancellationToken);

        return Result<PlacaAtivadaDto>.Success(new PlacaAtivadaDto(
            PlacaId: placa.Id,
            Slug: placa.Slug,
            EmpresaId: empresaId,
            UrlDestino: placa.UrlDestino!));
    }
}

public sealed class AtualizarUrlPlacaUseCase : IAtualizarUrlPlacaUseCase
{
    private readonly IPlacaRepository _placaRepository;
    private readonly IPlacaRedirecionamentoCache _cache;

    public AtualizarUrlPlacaUseCase(
        IPlacaRepository placaRepository,
        IPlacaRedirecionamentoCache cache)
    {
        _placaRepository = placaRepository;
        _cache = cache;
    }

    public async Task<Result> ExecutarAsync(
        Guid empresaId,
        AtualizarUrlPlacaRequest request,
        CancellationToken cancellationToken = default)
    {
        var placa = await _placaRepository.ObterPorIdAsync(request.PlacaId, cancellationToken);
        if (placa is null)
        {
            return Result.Failure(new Error("placa.naoEncontrada", "Placa não encontrada."));
        }

        if (placa.EmpresaId != empresaId)
        {
            return Result.Failure(new Error("placa.naoAutorizado", "A placa informada não pertence à empresa autenticada."));
        }

        var result = placa.AtualizarUrlDestino(request.NovaUrlDestino);
        if (result.IsFailure)
        {
            return result;
        }

        await _placaRepository.AtualizarAsync(placa, cancellationToken);
        await _cache.RemoverAsync(placa.Slug, cancellationToken);

        return Result.Success();
    }
}

public sealed class ConfigurarFiltroSatisfacaoUseCase : IConfigurarFiltroSatisfacaoUseCase
{
    private readonly IPlacaRepository _placaRepository;
    private readonly IPlacaRedirecionamentoCache _cache;

    public ConfigurarFiltroSatisfacaoUseCase(
        IPlacaRepository placaRepository,
        IPlacaRedirecionamentoCache cache)
    {
        _placaRepository = placaRepository;
        _cache = cache;
    }

    public async Task<Result> ExecutarAsync(
        Guid empresaId,
        ConfigurarFiltroSatisfacaoRequest request,
        CancellationToken cancellationToken = default)
    {
        var placa = await _placaRepository.ObterPorIdAsync(request.PlacaId, cancellationToken);
        if (placa is null)
        {
            return Result.Failure(new Error("placa.naoEncontrada", "Placa não encontrada."));
        }

        if (placa.EmpresaId != empresaId)
        {
            return Result.Failure(new Error("placa.naoAutorizado", "A placa informada não pertence à empresa autenticada."));
        }

        var result = placa.ConfigurarFiltroSatisfacao(request.Ativo, request.UrlSacInterno);
        if (result.IsFailure)
        {
            return result;
        }

        await _placaRepository.AtualizarAsync(placa, cancellationToken);
        await _cache.RemoverAsync(placa.Slug, cancellationToken);

        return Result.Success();
    }
}

public sealed class ListarPlacasClienteUseCase : IListarPlacasClienteUseCase
{
    private readonly IPlacaRepository _placaRepository;

    public ListarPlacasClienteUseCase(IPlacaRepository placaRepository)
    {
        _placaRepository = placaRepository;
    }

    public async Task<Result<IReadOnlyList<PlacaClienteDto>>> ExecutarAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        var placas = await _placaRepository.ObterPorEmpresaIdAsync(empresaId, cancellationToken);

        var dtos = placas.Select(p => new PlacaClienteDto(
            PlacaId: p.Id,
            Slug: p.Slug,
            NumeroLote: p.NumeroLote,
            UrlDestino: p.UrlDestino,
            StatusPlaca: p.StatusPlaca,
            ChipUid: p.ChipUid,
            DataCriacaoUtc: p.DataCriacaoUtc,
            DataAtivacaoUtc: p.DataAtivacaoUtc,
            FiltroSatisfacaoAtivo: p.FiltroSatisfacaoAtivo,
            UrlSacInterno: p.UrlSacInterno
        )).ToList();

        return Result<IReadOnlyList<PlacaClienteDto>>.Success(dtos);
    }
}

public sealed class ObterAnalyticsClienteUseCase : IObterAnalyticsClienteUseCase
{
    private readonly IPlacaRepository _placaRepository;

    public ObterAnalyticsClienteUseCase(IPlacaRepository placaRepository)
    {
        _placaRepository = placaRepository;
    }

    public async Task<Result<AnalyticsClienteDto>> ExecutarAsync(
        Guid empresaId,
        Guid? placaId,
        int dias,
        CancellationToken cancellationToken = default)
    {
        var diasValidos = dias switch
        {
            <= 7 => 7,
            <= 30 => 30,
            _ => 90
        };

        var analytics = await _placaRepository.ObterAnalyticsEmpresaAsync(
            empresaId,
            placaId,
            diasValidos,
            cancellationToken);

        return Result<AnalyticsClienteDto>.Success(analytics);
    }
}
