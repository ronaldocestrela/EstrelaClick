using EstrelaClick.Shared.SharedKernel.Results;
using FluentAssertions;

namespace EstrelaClick.Tests.UnitTests.SharedKernel.Results;

public class ResultTests
{
    [Fact]
    public void Error_None_DeveRepresentarAusenciaDeErro()
    {
        Error.None.Code.Should().Be("None");
        Error.None.Description.Should().BeEmpty();
    }

    [Fact]
    public void Result_Success_DeveRetornarSucessoSemErro()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Result_Failure_DeveRetornarFalhaComErroInformado()
    {
        var error = new Error("empresa.email.invalido", "Email inválido.");

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ResultOfT_Success_DeveRetornarValorEEstadoDeSucesso()
    {
        var result = Result<string>.Success("ok");

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be("ok");
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void ResultOfT_Failure_DeveRetornarErroESemValor()
    {
        var error = new Error("placa.slug.invalido", "Slug inválido.");

        var result = Result<string>.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.Value.Should().BeNull();
    }
}
