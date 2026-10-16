using System.Threading.Tasks;
using Bunit;
using EstrelaClick.Frontend.Shared.UI.Components.Forms;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI.Components;

public class FormInputTextTests : BunitContext
{
    [Fact]
    public void Render_ComLabelEPlaceholder_DeveExibirAtributos()
    {
        // Act
        var cut = Render<FormInputText>(
            builder => builder.Add(p => p.Label, "URL de Destino")
                              .Add(p => p.Placeholder, "https://exemplo.com/loja")
                              .Add(p => p.Value, "https://estrela.link"));

        // Assert
        cut.Find("label.input-label").TextContent.Should().Be("URL de Destino");
        var input = cut.Find("input.form-input");
        input.GetAttribute("placeholder").Should().Be("https://exemplo.com/loja");
        input.GetAttribute("value").Should().Be("https://estrela.link");
    }

    [Fact]
    public void Render_ComMensagemDeErro_DeveExibirErroEAplicarClasseInvalida()
    {
        // Act
        var cut = Render<FormInputText>(
            builder => builder.Add(p => p.Label, "Slug")
                              .Add(p => p.ErrorMessage, "Slug inválido ou já existente"));

        // Assert
        cut.Find("span.input-error-msg").TextContent.Should().Be("Slug inválido ou já existente");
        cut.Find("input.form-input").ClassList.Should().Contain("is-invalid");
    }

    [Fact]
    public async Task Input_AoDigitar_DeveDispararValueChanged()
    {
        // Arrange
        string? valorAtualizado = null;
        var cut = Render<FormInputText>(
            builder => builder.Add(p => p.Value, "antigo")
                              .Add(p => p.ValueChanged, (string? novo) => { valorAtualizado = novo; }));

        // Act
        var input = cut.Find("input.form-input");
        await input.InputAsync(new ChangeEventArgs { Value = "novo_slug" });

        // Assert
        valorAtualizado.Should().Be("novo_slug");
    }
}
