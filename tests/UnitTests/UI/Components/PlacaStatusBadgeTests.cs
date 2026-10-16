using System;
using Bunit;
using EstrelaClick.Frontend.Shared.UI.Components.Badges;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Components;

public class PlacaStatusBadgeTests : BunitContext
{
    [Theory]
    [InlineData(StatusPlaca.Criada, "Criada", "badge-neutral")]
    [InlineData(StatusPlaca.EmGravacao, "Em Gravação", "badge-info")]
    [InlineData(StatusPlaca.Gravada, "Gravada", "badge-success")]
    [InlineData(StatusPlaca.Vinculada, "Vinculada", "badge-primary")]
    public void Render_ComStatusDiferentes_DeveExibirTextoECssEsperados(
        StatusPlaca status,
        string textoEsperado,
        string classeCssEsperada)
    {
        // Act
        var cut = Render<PlacaStatusBadge>(
            builder => builder.Add(p => p.Status, status));

        // Assert
        cut.Markup.Should().Contain(textoEsperado);
        cut.Find("span.status-badge").ClassList.Should().Contain(classeCssEsperada);
    }

    [Fact]
    public void Render_StatusEmGravacao_DeveConterIndicadorDePulso()
    {
        // Act
        var cut = Render<PlacaStatusBadge>(
            builder => builder.Add(p => p.Status, StatusPlaca.EmGravacao));

        // Assert
        cut.Find("span.pulse-dot").Should().NotBeNull();
    }
}
