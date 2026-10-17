using Bunit;
using EstrelaClick.Frontend.Shared.UI.Components.Feedback;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Components;

public class LoadingSpinnerTests : BunitContext
{
    [Fact]
    public void Render_ComMessage_DeveExibirMensagem()
    {
        // Act
        var cut = Render<LoadingSpinner>(p => p
            .Add(s => s.Message, "Carregando dados..."));

        // Assert
        cut.Find("span.spinner-message").TextContent.Should().Be("Carregando dados...");
    }

    [Fact]
    public void Render_ComText_DeveExibirMensagemPelaPropriedadeText()
    {
        // Act
        var cut = Render<LoadingSpinner>(p => p
            .Add(s => s.Text, "Aguarde processamento..."));

        // Assert
        cut.Find("span.spinner-message").TextContent.Should().Be("Aguarde processamento...");
    }

    [Fact]
    public void Render_ComIsCentered_DeveAplicarClasseCentered()
    {
        // Act
        var cut = Render<LoadingSpinner>(p => p
            .Add(s => s.IsCentered, true));

        // Assert
        cut.Find("div.spinner-container").ClassList.Should().Contain("centered");
    }

    [Theory]
    [InlineData("sm", "size-sm")]
    [InlineData("md", "size-md")]
    [InlineData("lg", "size-lg")]
    public void Render_ComSize_DeveAplicarClasseTamanhoCorreta(string size, string expectedClass)
    {
        // Act
        var cut = Render<LoadingSpinner>(p => p
            .Add(s => s.Size, size));

        // Assert
        cut.Find("div.spinner").ClassList.Should().Contain(expectedClass);
    }
}
