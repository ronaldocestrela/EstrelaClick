using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using EstrelaClick.Frontend.Shared.UI.Components.Tables;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Components;

public class DataTableTests : BunitContext
{
    private record TestRow(string Id, string Nome, string Status);

    [Fact]
    public void Render_QuandoIsLoadingEhVerdadeiro_DeveExibirSpinnerDeCarregamento()
    {
        // Act
        var cut = Render<DataTable<TestRow>>(
            builder => builder.Add(p => p.IsLoading, true)
                              .Add(p => p.Items, new List<TestRow>()));

        // Assert
        cut.Find("div.table-loading").Should().NotBeNull();
    }

    [Fact]
    public void Render_QuandoListaEstaVazia_DeveExibirMensagemVazia()
    {
        // Act
        var cut = Render<DataTable<TestRow>>(
            builder => builder.Add(p => p.IsLoading, false)
                              .Add(p => p.Items, new List<TestRow>())
                              .Add(p => p.EmptyMessage, "Nenhuma placa encontrada"));

        // Assert
        cut.Markup.Should().Contain("Nenhuma placa encontrada");
    }

    [Fact]
    public void Render_ComItensECabecalho_DeveRenderizarTabelaCorretamente()
    {
        // Arrange
        var dados = new List<TestRow>
        {
            new("1", "Placa Alpha", "Criada"),
            new("2", "Placa Beta", "Gravada")
        };

        // Act
        var cut = Render<DataTable<TestRow>>(
            builder => builder.Add(p => p.IsLoading, false)
                              .Add(p => p.Items, dados)
                              .Add(p => p.HeaderTemplate, (RenderFragment)(b =>
                              {
                                  b.OpenElement(0, "tr");
                                  b.OpenElement(1, "th");
                                  b.AddContent(2, "Nome");
                                  b.CloseElement();
                                  b.CloseElement();
                              }))
                              .Add(p => p.RowTemplate, (RenderFragment<TestRow>)(item => b =>
                              {
                                  b.OpenElement(0, "tr");
                                  b.OpenElement(1, "td");
                                  b.AddContent(2, item.Nome);
                                  b.CloseElement();
                                  b.CloseElement();
                              })));

        // Assert
        cut.Find("th").TextContent.Should().Be("Nome");
        var rows = cut.FindAll("tbody tr");
        rows.Should().HaveCount(2);
        cut.Markup.Should().Contain("Placa Alpha");
        cut.Markup.Should().Contain("Placa Beta");
    }

    [Fact]
    public async Task Input_BuscaRapida_DeveDispararEventoOnSearch()
    {
        // Arrange
        string? termoBuscado = null;
        var cut = Render<DataTable<TestRow>>(
            builder => builder.Add(p => p.Items, new List<TestRow>())
                              .Add(p => p.ShowSearch, true)
                              .Add(p => p.OnSearch, (string query) => { termoBuscado = query; }));

        // Act
        var input = cut.Find("input.search-input");
        await input.InputAsync(new ChangeEventArgs { Value = "lote-01" });

        // Assert
        termoBuscado.Should().Be("lote-01");
    }
}
