using System.Threading.Tasks;
using Bunit;
using EstrelaClick.Frontend.Shared.UI.Components.Tables;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Components;

public class PaginationControlTests : BunitContext
{
    [Fact]
    public void Render_ComPaginasEMetricas_DeveExibirResumoCorreto()
    {
        // Act: Página 2 de 5, com 10 itens por página, total 45 itens
        var cut = Render<PaginationControl>(
            builder => builder.Add(p => p.CurrentPage, 2)
                              .Add(p => p.TotalPages, 5)
                              .Add(p => p.PageSize, 10)
                              .Add(p => p.TotalItems, 45));

        // Assert
        cut.Markup.Should().Contain("Exibindo 11 a 20 de 45 registros");
    }

    [Fact]
    public void Render_PrimeiraPagina_BotaoAnteriorDeveEstarDesabilitado()
    {
        // Act
        var cut = Render<PaginationControl>(
            builder => builder.Add(p => p.CurrentPage, 1)
                              .Add(p => p.TotalPages, 3));

        // Assert
        cut.Find("button.btn-prev").HasAttribute("disabled").Should().BeTrue();
        cut.Find("button.btn-next").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Render_UltimaPagina_BotaoProximoDeveEstarDesabilitado()
    {
        // Act
        var cut = Render<PaginationControl>(
            builder => builder.Add(p => p.CurrentPage, 3)
                              .Add(p => p.TotalPages, 3));

        // Assert
        cut.Find("button.btn-prev").HasAttribute("disabled").Should().BeFalse();
        cut.Find("button.btn-next").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public async Task Click_BotaoProximo_DeveEmitirOnPageChangedComProximaPagina()
    {
        // Arrange
        var paginaSelecionada = 0;
        var cut = Render<PaginationControl>(
            builder => builder.Add(p => p.CurrentPage, 2)
                              .Add(p => p.TotalPages, 5)
                              .Add(p => p.OnPageChanged, (int page) => { paginaSelecionada = page; }));

        // Act
        await cut.Find("button.btn-next").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        paginaSelecionada.Should().Be(3);
    }
}
