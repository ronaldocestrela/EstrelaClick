using EstrelaClick.Shared.SharedKernel.Mobile;
using EstrelaClick.Shared.SharedKernel.Mobile.ViewModels;
using EstrelaClick.Shared.SharedKernel.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Mobile;

public class BancadaViewModelTests
{
    private readonly IFactoryApiClient _apiClientMock = Substitute.For<IFactoryApiClient>();
    private readonly IOperatorSessionService _sessionService = new OperatorSessionService();

    public BancadaViewModelTests()
    {
        _sessionService.IniciarSessao("OP01", "Lucas Operador", "token-jwt", DateTime.UtcNow.AddHours(8));
    }

    [Fact]
    public void Inicializacao_DeveCarregarDadosDoOperadorDaSessao()
    {
        // Arrange & Act
        var vm = new BancadaViewModel(_apiClientMock, _sessionService);

        // Assert
        vm.OperadorId.Should().Be("OP01");
        vm.OperadorNome.Should().Be("Lucas Operador");
        vm.ModoGravacaoContinua.Should().BeTrue();
        vm.GravadasNaSessao.Should().Be(0);
        vm.TemPlacaAtiva.Should().BeFalse();
    }

    [Fact]
    public void AlternarModoGravacaoContinua_DeveInverterValorEAtualizarStatus()
    {
        // Arrange
        var vm = new BancadaViewModel(_apiClientMock, _sessionService);

        // Act - Da primeira vez inverte de true para false
        vm.AlternarModoGravacaoContinua();

        // Assert
        vm.ModoGravacaoContinua.Should().BeFalse();
        vm.StatusMensagem.Should().Contain("Manual");

        // Act 2 - Da segunda vez inverte de false para true
        vm.AlternarModoGravacaoContinua();

        // Assert 2
        vm.ModoGravacaoContinua.Should().BeTrue();
        vm.StatusMensagem.Should().Contain("Contínua");
    }

    [Fact]
    public async Task CarregarProximaPlacaAsync_QuandoSucesso_DeveAtualizarPlacaAtiva()
    {
        // Arrange
        var placa = new PlacaAtivaInfo(
            Guid.NewGuid(),
            "slug1234567890123456789012345678",
            "LOTE-2026-01",
            "https://estrelaclick.com/r/slug1234567890123456789012345678",
            DateTime.UtcNow,
            DateTime.UtcNow.AddSeconds(90),
            "OP01");

        _apiClientMock.ObterProximaPendenteAsync("LOTE-2026-01", Arg.Any<CancellationToken>())
            .Returns(Result<PlacaAtivaInfo>.Success(placa));

        var vm = new BancadaViewModel(_apiClientMock, _sessionService)
        {
            NumeroLote = "LOTE-2026-01"
        };

        // Act
        var result = await vm.CarregarProximaPlacaAsync();

        // Assert
        result.Should().BeTrue();
        vm.TemPlacaAtiva.Should().BeTrue();
        vm.PlacaAtivaSlug.Should().Be("slug1234567890123456789012345678");
        vm.PlacaAtivaUrl.Should().Be("https://estrelaclick.com/r/slug1234567890123456789012345678");
        vm.StatusMensagem.Should().Contain("Aproxime");
    }

    [Fact]
    public async Task CarregarResumoLoteAsync_QuandoSucesso_DeveAtualizarMetricasDeLote()
    {
        // Arrange
        var resumo = new ResumoLoteInfo("LOTE-01", 100, 30, 70, 0);
        _apiClientMock.ObterResumoLoteAsync("LOTE-01", Arg.Any<CancellationToken>())
            .Returns(Result<ResumoLoteInfo>.Success(resumo));

        var vm = new BancadaViewModel(_apiClientMock, _sessionService)
        {
            NumeroLote = "LOTE-01"
        };

        // Act
        var result = await vm.CarregarResumoLoteAsync();

        // Assert
        result.Should().BeTrue();
        vm.TotalLote.Should().Be(100);
        vm.PendentesLote.Should().Be(70);
    }
}
