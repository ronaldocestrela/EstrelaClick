using Bunit;
using EstrelaClick.Frontend.Shared.UI.Components.Feedback;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Components;

public class EmptyStateTests : BunitContext
{
    [Fact]
    public void Render_ComTitleEDescription_DeveExibirTextoCorretamente()
    {
        // Act
        var cut = Render<EmptyState>(p => p
            .Add(e => e.Title, "Nenhum item")
            .Add(e => e.Description, "Adicione novos itens para visualizar."));

        // Assert
        cut.Find("h4.empty-title").TextContent.Should().Be("Nenhum item");
        cut.Find("p.empty-description").TextContent.Should().Be("Adicione novos itens para visualizar.");
    }

    [Fact]
    public void Render_ComMessage_DeveExibirDescricaoPelaPropriedadeMessage()
    {
        // Act
        var cut = Render<EmptyState>(p => p
            .Add(e => e.Title, "Nenhum registro")
            .Add(e => e.Message, "Mensagem alternativa de vazio"));

        // Assert
        cut.Find("h4.empty-title").TextContent.Should().Be("Nenhum registro");
        cut.Find("p.empty-description").TextContent.Should().Be("Mensagem alternativa de vazio");
    }
}
