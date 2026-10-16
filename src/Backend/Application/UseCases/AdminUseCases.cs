using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Admin;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.UseCases;

public sealed class ObterMetricasFabricaUseCase : IObterMetricasFabricaUseCase
{
    private readonly IPlacaRepository _placaRepository;

    public ObterMetricasFabricaUseCase(IPlacaRepository placaRepository)
    {
        _placaRepository = placaRepository;
    }

    public async Task<Result<MetricasFabricaDto>> ExecutarAsync(CancellationToken cancellationToken = default)
    {
        var metricas = await _placaRepository.ObterMetricasFabricaAsync(cancellationToken);
        return Result<MetricasFabricaDto>.Success(metricas);
    }
}

public sealed class ListarLotesAdminUseCase : IListarLotesAdminUseCase
{
    private readonly IPlacaRepository _placaRepository;

    public ListarLotesAdminUseCase(IPlacaRepository placaRepository)
    {
        _placaRepository = placaRepository;
    }

    public async Task<Result<IReadOnlyList<LoteItemDto>>> ExecutarAsync(CancellationToken cancellationToken = default)
    {
        var lotes = await _placaRepository.ListarResumoLotesAsync(cancellationToken);
        return Result<IReadOnlyList<LoteItemDto>>.Success(lotes);
    }
}

public sealed class ConsultarAuditoriaPlacaUseCase : IConsultarAuditoriaPlacaUseCase
{
    private readonly IPlacaRepository _placaRepository;
    private readonly IEmpresaRepository _empresaRepository;

    public ConsultarAuditoriaPlacaUseCase(IPlacaRepository placaRepository, IEmpresaRepository empresaRepository)
    {
        _placaRepository = placaRepository;
        _empresaRepository = empresaRepository;
    }

    public async Task<Result<PlacaAuditoriaDto>> ExecutarAsync(string termoBusca, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(termoBusca))
        {
            return Result<PlacaAuditoriaDto>.Failure(PlacaErrors.SlugInvalido);
        }

        var placa = await _placaRepository.ObterPorSlugOuUidAsync(termoBusca.Trim(), cancellationToken);
        if (placa is null)
        {
            return Result<PlacaAuditoriaDto>.Failure(PlacaErrors.NaoEncontrada);
        }

        string? razaoSocial = null;
        if (placa.EmpresaId.HasValue)
        {
            var empresa = await _empresaRepository.ObterPorIdAsync(placa.EmpresaId.Value, cancellationToken);
            razaoSocial = empresa?.RazaoSocial;
        }

        var totalAcessos = await _placaRepository.ObterTotalAcessosPlacaAsync(placa.Id, cancellationToken);

        var dto = new PlacaAuditoriaDto(
            Id: placa.Id,
            Slug: placa.Slug,
            NumeroLote: placa.NumeroLote,
            Status: placa.StatusPlaca,
            ChipUid: placa.ChipUid,
            UrlDestino: placa.UrlDestino,
            EmpresaId: placa.EmpresaId,
            EmpresaRazaoSocial: razaoSocial,
            OperadorReservaId: placa.OperadorReservaId,
            DataCriacaoUtc: placa.DataCriacaoUtc,
            DataGravacaoUtc: placa.DataGravacaoUtc,
            DataAtivacaoUtc: placa.DataAtivacaoUtc,
            TotalAcessos: totalAcessos
        );

        return Result<PlacaAuditoriaDto>.Success(dto);
    }
}

public sealed class ListarEmpresasAdminUseCase : IListarEmpresasAdminUseCase
{
    private readonly IEmpresaRepository _empresaRepository;

    public ListarEmpresasAdminUseCase(IEmpresaRepository empresaRepository)
    {
        _empresaRepository = empresaRepository;
    }

    public async Task<Result<IReadOnlyList<EmpresaDto>>> ExecutarAsync(CancellationToken cancellationToken = default)
    {
        var empresas = await _empresaRepository.ListarTodasAsync(cancellationToken);
        var dtos = new List<EmpresaDto>();

        foreach (var empresa in empresas)
        {
            var totalPlacas = await _empresaRepository.ObterTotalPlacasPorEmpresaAsync(empresa.Id, cancellationToken);
            dtos.Add(new EmpresaDto(
                Id: empresa.Id,
                RazaoSocial: empresa.RazaoSocial,
                Email: empresa.Email,
                Telefone: empresa.Telefone,
                StatusAtivo: empresa.StatusAtivo,
                DataCadastroUtc: empresa.DataCadastroUtc,
                TotalPlacas: totalPlacas
            ));
        }

        return Result<IReadOnlyList<EmpresaDto>>.Success(dtos);
    }
}

public sealed class CadastrarEmpresaAdminUseCase : ICadastrarEmpresaAdminUseCase
{
    private readonly IEmpresaRepository _empresaRepository;

    public CadastrarEmpresaAdminUseCase(IEmpresaRepository empresaRepository)
    {
        _empresaRepository = empresaRepository;
    }

    public async Task<Result<EmpresaDto>> ExecutarAsync(CriarEmpresaRequest request, CancellationToken cancellationToken = default)
    {
        var criacaoResult = Empresa.Criar(request.RazaoSocial, request.Email, request.Telefone);
        if (criacaoResult.IsFailure || criacaoResult.Value is null)
        {
            return Result<EmpresaDto>.Failure(criacaoResult.Error);
        }

        var empresa = criacaoResult.Value;
        await _empresaRepository.InserirAsync(empresa, cancellationToken);

        var dto = new EmpresaDto(
            Id: empresa.Id,
            RazaoSocial: empresa.RazaoSocial,
            Email: empresa.Email,
            Telefone: empresa.Telefone,
            StatusAtivo: empresa.StatusAtivo,
            DataCadastroUtc: empresa.DataCadastroUtc,
            TotalPlacas: 0
        );

        return Result<EmpresaDto>.Success(dto);
    }
}
