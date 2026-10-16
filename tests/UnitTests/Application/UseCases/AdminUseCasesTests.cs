using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Application.UseCases;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Admin;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Application.UseCases;

public class AdminUseCasesTests
{
    private readonly IPlacaRepository _placaRepositoryMock = Substitute.For<IPlacaRepository>();
    private readonly IEmpresaRepository _empresaRepositoryMock = Substitute.For<IEmpresaRepository>();

    [Fact]
    public async Task ObterMetricasFabrica_DeveRetornarMetricasComSucesso()
    {
        // Arrange
        var metricasEsperadas = new MetricasFabricaDto(
            TotalPlacas: 100,
            TotalCriadas: 40,
            TotalEmGravacao: 10,
            TotalGravadas: 30,
            TotalVinculadas: 20,
            TotalLotes: 5
        );
        _placaRepositoryMock.ObterMetricasFabricaAsync(Arg.Any<CancellationToken>())
            .Returns(metricasEsperadas);

        var useCase = new ObterMetricasFabricaUseCase(_placaRepositoryMock);

        // Act
        var result = await useCase.ExecutarAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(metricasEsperadas);
    }

    [Fact]
    public async Task ListarLotesAdmin_DeveRetornarListaDeLotesComSucesso()
    {
        // Arrange
        var lotesEsperados = new List<LoteItemDto>
        {
            new("LOTE-001", 50, 10, 5, 20, 15, DateTime.UtcNow)
        };
        _placaRepositoryMock.ListarResumoLotesAsync(Arg.Any<CancellationToken>())
            .Returns(lotesEsperados);

        var useCase = new ListarLotesAdminUseCase(_placaRepositoryMock);

        // Act
        var result = await useCase.ExecutarAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(lotesEsperados);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ConsultarAuditoriaPlaca_QuandoTermoVazio_DeveRetornarFalha(string termo)
    {
        // Arrange
        var useCase = new ConsultarAuditoriaPlacaUseCase(_placaRepositoryMock, _empresaRepositoryMock);

        // Act
        var result = await useCase.ExecutarAsync(termo);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.SlugInvalido);
    }

    [Fact]
    public async Task ConsultarAuditoriaPlaca_QuandoPlacaNaoEncontrada_DeveRetornarFalha()
    {
        // Arrange
        _placaRepositoryMock.ObterPorSlugOuUidAsync("INEXISTENTE", Arg.Any<CancellationToken>())
            .Returns((Placa?)null);

        var useCase = new ConsultarAuditoriaPlacaUseCase(_placaRepositoryMock, _empresaRepositoryMock);

        // Act
        var result = await useCase.ExecutarAsync("INEXISTENTE");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.NaoEncontrada);
    }

    [Fact]
    public async Task ConsultarAuditoriaPlaca_QuandoEncontrada_DeveRetornarDtoCompleto()
    {
        // Arrange
        var slug = "1234567890abcdef1234567890abcdef";
        var placa = Placa.Criar(slug, "LOTE-01").Value!;
        _placaRepositoryMock.ObterPorSlugOuUidAsync(slug, Arg.Any<CancellationToken>())
            .Returns(placa);
        _placaRepositoryMock.ObterTotalAcessosPlacaAsync(placa.Id, Arg.Any<CancellationToken>())
            .Returns(42);

        var useCase = new ConsultarAuditoriaPlacaUseCase(_placaRepositoryMock, _empresaRepositoryMock);

        // Act
        var result = await useCase.ExecutarAsync(slug);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Slug.Should().Be(slug);
        result.Value.NumeroLote.Should().Be("LOTE-01");
        result.Value.TotalAcessos.Should().Be(42);
    }

    [Fact]
    public async Task CadastrarEmpresaAdmin_ComDadosValidos_DeveSalvarERetornarDto()
    {
        // Arrange
        var useCase = new CadastrarEmpresaAdminUseCase(_empresaRepositoryMock);
        var request = new CriarEmpresaRequest("Loja Estrela LTDA", "contato@lojaestrela.com.br", "(11) 99999-8888");

        // Act
        var result = await useCase.ExecutarAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RazaoSocial.Should().Be("Loja Estrela LTDA");
        result.Value.Email.Should().Be("contato@lojaestrela.com.br");
        await _empresaRepositoryMock.Received(1).InserirAsync(Arg.Any<Empresa>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CadastrarEmpresaAdmin_ComEmailInvalido_DeveRetornarFalha()
    {
        // Arrange
        var useCase = new CadastrarEmpresaAdminUseCase(_empresaRepositoryMock);
        var request = new CriarEmpresaRequest("Loja Estrela LTDA", "email-invalido", "(11) 99999-8888");

        // Act
        var result = await useCase.ExecutarAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("empresa.email.invalido");
    }

    [Fact]
    public async Task ListarEmpresasAdmin_DeveRetornarListaComTotalDePlacas()
    {
        // Arrange
        var empresa = Empresa.Criar("Padaria Estrela", "padaria@estrela.com", "(11) 3333-2222").Value!;
        _empresaRepositoryMock.ListarTodasAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Empresa> { empresa });
        _empresaRepositoryMock.ObterTotalPlacasPorEmpresaAsync(empresa.Id, Arg.Any<CancellationToken>())
            .Returns(15);

        var useCase = new ListarEmpresasAdminUseCase(_empresaRepositoryMock);

        // Act
        var result = await useCase.ExecutarAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].RazaoSocial.Should().Be("Padaria Estrela");
        result.Value[0].TotalPlacas.Should().Be(15);
    }
}
