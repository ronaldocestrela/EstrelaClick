using Bunit;
using EstrelaClick.Frontend.ClientDashboard.Components.Pages;
using EstrelaClick.Frontend.ClientDashboard.Services;
using EstrelaClick.Shared.SharedKernel.Client;
using EstrelaClick.Shared.SharedKernel.Enums;
using EstrelaClick.Shared.SharedKernel.Results;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Pages;

public class ClientPagesTests : BunitContext
{
    private readonly IClientApiClient _clientApiMock = Substitute.For<IClientApiClient>();

    public ClientPagesTests()
    {
        Services.AddSingleton(_clientApiMock);
    }

    [Fact]
    public void AtivarPlaca_DeveRenderizarFormularioDeAtivacao()
    {
        // Act
        var cut = Render<AtivarPlaca>();

        // Assert
        cut.Markup.Should().Contain("Ativação de Placa Inteligente");
        cut.Markup.Should().Contain("URL de Destino Inicial");
        cut.Markup.Should().Contain("Ativar Placa Agora");
    }

    [Fact]
    public void Placas_DeveRenderizarTabelaEBotaoNovaPlaca()
    {
        // Arrange
        var placas = new List<PlacaItemDto>
        {
            new(Guid.NewGuid(), "11223344556677889900aabbccddeeff", "LOTE-01", "https://google.com/review", StatusPlaca.Vinculada, "UID-123", DateTime.UtcNow, DateTime.UtcNow, false, null)
        };

        _clientApiMock.ListarPlacasAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<PlacaItemDto>>.Success(placas));

        // Act
        var cut = Render<Placas>();

        // Assert
        cut.Markup.Should().Contain("Minhas Placas Inteligentes");
        cut.Markup.Should().Contain("Ativar Nova Placa");
        cut.Markup.Should().Contain("https://google.com/review");
        cut.Markup.Should().Contain("Editar Link");
    }

    [Fact]
    public void Analytics_DeveRenderizarMetricasETitulo()
    {
        // Arrange
        var analytics = new AnalyticsDto(
            TotalToquesPeriodo: 450,
            TotalToquesHoje: 32,
            MediaDiariaToques: 15.0,
            ToquesPorDia: new List<ToquesDiaDto> { new(DateOnly.FromDateTime(DateTime.UtcNow), 32) },
            ToquesPorHora: new List<ToquesHoraDto> { new(12, 10), new(13, 22) },
            Dispositivos: new List<DispositivoContagemDto> { new("Android", 300, 66.7), new("iOS (Apple)", 150, 33.3) },
            UltimosAcessos: new List<UltimoAcessoDto> { new(DateTime.UtcNow, "Android", null) }
        );

        _clientApiMock.ObterAnalyticsAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<AnalyticsDto>.Success(analytics));

        // Act
        var cut = Render<Analytics>();

        // Assert
        cut.Markup.Should().Contain("Analytics & Métricas de Toque");
        cut.Markup.Should().Contain("Total de Toques no Período");
        cut.Markup.Should().Contain("450");
        cut.Markup.Should().Contain("32");
        cut.Markup.Should().Contain("Horários de Pico");
        cut.Markup.Should().Contain("Distribuição de Dispositivos");
    }

    [Fact]
    public void FeedbackGate_DeveRenderizarEstrelasDeAvaliacao()
    {
        // Arrange
        var slug = Guid.NewGuid().ToString("N");
        var placa = new VerificarOnboardingDto(
            PlacaId: Guid.NewGuid(),
            Slug: slug,
            StatusPlaca: "Vinculada",
            DisponivelParaAtivacao: false,
            UrlDestinoAtual: "https://g.page/review",
            FiltroSatisfacaoAtivo: true,
            UrlSacInterno: "https://wa.me/5511999999999"
        );

        _clientApiMock.VerificarOnboardingAsync(slug, Arg.Any<CancellationToken>())
            .Returns(Result<VerificarOnboardingDto>.Success(placa));

        // Act
        var cut = Render<FeedbackGate>(parameters => parameters.Add(p => p.Slug, slug));

        // Assert
        cut.Markup.Should().Contain("Como foi sua experiência conosco?");
        cut.Markup.Should().Contain("⭐");
    }
}
