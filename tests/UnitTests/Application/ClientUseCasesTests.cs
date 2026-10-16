using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Application.UseCases;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;
using NSubstitute;

namespace EstrelaClick.Tests.UnitTests.Application;

public class ClientUseCasesTests
{
    private readonly IPlacaRepository _placaRepository = Substitute.For<IPlacaRepository>();
    private readonly IEmpresaRepository _empresaRepository = Substitute.For<IEmpresaRepository>();
    private readonly IPlacaRedirecionamentoCache _cache = Substitute.For<IPlacaRedirecionamentoCache>();

    #region AtivarPlacaOnboardingUseCase

    [Fact]
    public async Task AtivarPlacaOnboarding_DeveAtivarComSucesso_QuandoEmpresaForNova()
    {
        var slug = Guid.NewGuid().ToString("N");
        var placa = Placa.Criar(slug).Value!;
        placa.RegistrarGravacao("04AABBCCDD");

        _placaRepository
            .ObterPorSlugParaAtualizacaoAsync(slug, Arg.Any<CancellationToken>())
            .Returns(placa);

        var useCase = new AtivarPlacaOnboardingUseCase(
            _placaRepository,
            _empresaRepository,
            _cache);

        var request = new AtivarPlacaOnboardingRequest(
            Slug: slug,
            UrlDestino: "https://g.page/r/minha-empresa/review",
            EmpresaExistenteId: null,
            RazaoSocial: "Padaria Estrela LTDA",
            Email: "contato@padariaestrela.com.br",
            Telefone: "11988887777");

        var result = await useCase.ExecutarAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Slug.Should().Be(slug);
        result.Value.UrlDestino.Should().Be("https://g.page/r/minha-empresa/review");

        placa.StatusPlaca.Should().Be(StatusPlaca.Vinculada);
        await _empresaRepository.Received(1).InserirAsync(Arg.Any<Empresa>(), Arg.Any<CancellationToken>());
        await _placaRepository.Received(1).AtualizarAsync(placa, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoverAsync(slug, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AtivarPlacaOnboarding_DeveAtivarComSucesso_QuandoEmpresaJaExistir()
    {
        var slug = Guid.NewGuid().ToString("N");
        var placa = Placa.Criar(slug).Value!;
        placa.RegistrarGravacao("04AABBCCDD");

        var empresaId = Guid.NewGuid();
        var empresaExistente = Empresa.Criar("Empresa Existente", "teste@empresa.com", "11999998888").Value!;

        _placaRepository
            .ObterPorSlugParaAtualizacaoAsync(slug, Arg.Any<CancellationToken>())
            .Returns(placa);

        _empresaRepository
            .ObterPorIdAsync(empresaId, Arg.Any<CancellationToken>())
            .Returns(empresaExistente);

        var useCase = new AtivarPlacaOnboardingUseCase(
            _placaRepository,
            _empresaRepository,
            _cache);

        var request = new AtivarPlacaOnboardingRequest(
            Slug: slug,
            UrlDestino: "https://wa.me/5511999998888",
            EmpresaExistenteId: empresaId);

        var result = await useCase.ExecutarAsync(request);

        result.IsSuccess.Should().BeTrue();
        placa.StatusPlaca.Should().Be(StatusPlaca.Vinculada);
        placa.EmpresaId.Should().Be(empresaExistente.Id);
        await _empresaRepository.DidNotReceive().InserirAsync(Arg.Any<Empresa>(), Arg.Any<CancellationToken>());
        await _placaRepository.Received(1).AtualizarAsync(placa, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoverAsync(slug, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AtivarPlacaOnboarding_DeveFalhar_QuandoPlacaNaoEstiverGravada()
    {
        var slug = Guid.NewGuid().ToString("N");
        var placa = Placa.Criar(slug).Value!; // Status Criada

        _placaRepository
            .ObterPorSlugParaAtualizacaoAsync(slug, Arg.Any<CancellationToken>())
            .Returns(placa);

        var useCase = new AtivarPlacaOnboardingUseCase(
            _placaRepository,
            _empresaRepository,
            _cache);

        var request = new AtivarPlacaOnboardingRequest(
            Slug: slug,
            UrlDestino: "https://g.page/review",
            RazaoSocial: "Padaria LTDA",
            Email: "padaria@teste.com",
            Telefone: "11999999999");

        var result = await useCase.ExecutarAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.onboarding.statusInvalido");
    }

    #endregion

    #region AtualizarUrlPlacaUseCase

    [Fact]
    public async Task AtualizarUrlPlaca_DeveAtualizarERemoverCache_QuandoPlacaPertencerAEmpresa()
    {
        var empresaId = Guid.NewGuid();
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
        placa.RegistrarGravacao("04AABBCCDD");
        placa.VincularEmpresa(empresaId, "https://url-antiga.com");

        _placaRepository
            .ObterPorIdAsync(placa.Id, Arg.Any<CancellationToken>())
            .Returns(placa);

        var useCase = new AtualizarUrlPlacaUseCase(
            _placaRepository,
            _cache);

        var request = new AtualizarUrlPlacaRequest(placa.Id, "https://nova-url.com/cardapio");

        var result = await useCase.ExecutarAsync(empresaId, request);

        result.IsSuccess.Should().BeTrue();
        placa.UrlDestino.Should().Be("https://nova-url.com/cardapio");
        await _placaRepository.Received(1).AtualizarAsync(placa, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoverAsync(placa.Slug, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AtualizarUrlPlaca_DeveFalhar_QuandoPlacaNaoPertencerAEmpresa()
    {
        var empresaDona = Guid.NewGuid();
        var outraEmpresa = Guid.NewGuid();

        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
        placa.RegistrarGravacao("04AABBCCDD");
        placa.VincularEmpresa(empresaDona, "https://url-antiga.com");

        _placaRepository
            .ObterPorIdAsync(placa.Id, Arg.Any<CancellationToken>())
            .Returns(placa);

        var useCase = new AtualizarUrlPlacaUseCase(
            _placaRepository,
            _cache);

        var request = new AtualizarUrlPlacaRequest(placa.Id, "https://nova-url.com");

        var result = await useCase.ExecutarAsync(outraEmpresa, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.naoAutorizado");
    }

    #endregion

    #region ConfigurarFiltroSatisfacaoUseCase

    [Fact]
    public async Task ConfigurarFiltroSatisfacao_DeveAtualizarFiltroERemoverCache_QuandoPlacaPertencerAEmpresa()
    {
        var empresaId = Guid.NewGuid();
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
        placa.RegistrarGravacao("04AABBCCDD");
        placa.VincularEmpresa(empresaId, "https://url-destino.com");

        _placaRepository
            .ObterPorIdAsync(placa.Id, Arg.Any<CancellationToken>())
            .Returns(placa);

        var useCase = new ConfigurarFiltroSatisfacaoUseCase(
            _placaRepository,
            _cache);

        var request = new ConfigurarFiltroSatisfacaoRequest(
            PlacaId: placa.Id,
            Ativo: true,
            UrlSacInterno: "https://wa.me/5511999999999");

        var result = await useCase.ExecutarAsync(empresaId, request);

        result.IsSuccess.Should().BeTrue();
        placa.FiltroSatisfacaoAtivo.Should().BeTrue();
        placa.UrlSacInterno.Should().Be("https://wa.me/5511999999999");
        await _cache.Received(1).RemoverAsync(placa.Slug, Arg.Any<CancellationToken>());
    }

    #endregion

    #region ListarPlacasClienteUseCase

    [Fact]
    public async Task ListarPlacasCliente_DeveRetornarPlacasMapeadasDaEmpresa()
    {
        var empresaId = Guid.NewGuid();
        var placa1 = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
        placa1.RegistrarGravacao("04AABBCCDD");
        placa1.VincularEmpresa(empresaId, "https://link1.com");

        var placa2 = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
        placa2.RegistrarGravacao("04EEFFGGHH");
        placa2.VincularEmpresa(empresaId, "https://link2.com");

        _placaRepository
            .ObterPorEmpresaIdAsync(empresaId, Arg.Any<CancellationToken>())
            .Returns(new List<Placa> { placa1, placa2 });

        var useCase = new ListarPlacasClienteUseCase(_placaRepository);

        var result = await useCase.ExecutarAsync(empresaId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].PlacaId.Should().Be(placa1.Id);
        result.Value[1].PlacaId.Should().Be(placa2.Id);
    }

    #endregion

    #region ObterAnalyticsClienteUseCase

    [Fact]
    public async Task ObterAnalyticsCliente_DeveConsultarRepositorioERetornarMetricas()
    {
        var empresaId = Guid.NewGuid();
        var esperado = new AnalyticsClienteDto(
            TotalToquesPeriodo: 150,
            TotalToquesHoje: 12,
            MediaDiariaToques: 5.0,
            ToquesPorDia: new List<ToquesDiaDto> { new(DateOnly.FromDateTime(DateTime.UtcNow), 12) },
            ToquesPorHora: new List<ToquesHoraDto> { new(14, 8), new(15, 4) },
            Dispositivos: new List<DispositivoContagemDto> { new("Android", 90, 60.0), new("iOS", 60, 40.0) },
            UltimosAcessos: new List<UltimoAcessoDto> { new(DateTime.UtcNow, "Android", null) });

        _placaRepository
            .ObterAnalyticsEmpresaAsync(empresaId, null, 30, Arg.Any<CancellationToken>())
            .Returns(esperado);

        var useCase = new ObterAnalyticsClienteUseCase(_placaRepository);

        var result = await useCase.ExecutarAsync(empresaId, null, 30);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(esperado);
    }

    #endregion
}
