using FluentAssertions;
using NetArchTest.Rules;

namespace EstrelaClick.Tests.ArchitectureTests;

public class LayeringRulesTests
{
    [Fact]
    public void Domain_Nao_Deve_Depender_De_Camadas_Externas()
    {
        var result = Types
            .InAssembly(typeof(EstrelaClick.Backend.Domain.Entities.Placa).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EstrelaClick.Backend.Application",
                "EstrelaClick.Backend.Infrastructure",
                "EstrelaClick.Backend.Api",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Nao_Deve_Depender_De_Infrastructure_Ou_Api()
    {
        var result = Types
            .InAssembly(typeof(EstrelaClick.Backend.Application.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EstrelaClick.Backend.Infrastructure",
                "EstrelaClick.Backend.Api",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
