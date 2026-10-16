using Bunit;
using EstrelaClick.Frontend.Shared.UI.Components.Metrics;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Components;

public class MetricCardTests : BunitContext
{
    [Fact]
    public void Render_ComTituloEValor_DeveRenderizarElementosCorretamente()
    {
        // Act
        var cut = Render<MetricCard>(
            builder => builder.Add(p => p.Title, "Total de Placas Ativas")
                              .Add(p => p.Value, "1.450")
                              .Add(p => p.Subtitle, "No período selecionado"));

        // Assert
        cut.Find("h4.metric-title").TextContent.Should().Be("Total de Placas Ativas");
        cut.Find("div.metric-value").TextContent.Should().Be("1.450");
        cut.Find("span.metric-subtitle").TextContent.Should().Be("No período selecionado");
    }

    [Fact]
    public void Render_ComTendenciaPositiva_DeveExibirBadgeComClassePositiva()
    {
        // Act
        var cut = Render<MetricCard>(
            builder => builder.Add(p => p.Title, "Acessos NFC")
                              .Add(p => p.Value, "320")
                              .Add(p => p.TrendText, "+12% vs mês anterior")
                              .Add(p => p.IsPositiveTrend, true));

        // Assert
        var trendEl = cut.Find("span.metric-trend");
        trendEl.TextContent.Should().Contain("+12%");
        trendEl.ClassList.Should().Contain("trend-positive");
    }

    [Fact]
    public void Render_ComTendenciaNegativa_DeveExibirBadgeComClasseNegativa()
    {
        // Act
        var cut = Render<MetricCard>(
            builder => builder.Add(p => p.Title, "Falhas de Gravação")
                              .Add(p => p.Value, "5")
                              .Add(p => p.TrendText, "-3% vs ontem")
                              .Add(p => p.IsPositiveTrend, false));

        // Assert
        var trendEl = cut.Find("span.metric-trend");
        trendEl.ClassList.Should().Contain("trend-negative");
    }
}
