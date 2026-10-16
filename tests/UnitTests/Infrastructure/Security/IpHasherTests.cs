using EstrelaClick.Backend.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Infrastructure.Security;

public sealed class IpHasherTests
{
    [Fact]
    public void HashIp_ComMesmoIp_DeveGerarMesmoHashDeterminístico()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:IpHashSalt"] = "TestSalt_123"
            })
            .Build();

        var hasher = new IpHasher(configuration);
        const string ip = "192.168.1.50";

        // Act
        var hash1 = hasher.HashIp(ip);
        var hash2 = hasher.HashIp(ip);

        // Assert
        hash1.Should().NotBeNullOrWhiteSpace();
        hash1.Length.Should().Be(64); // SHA-256 hex string
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashIp_ComIpsDiferentes_DeveGerarHashesDiferentes()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:IpHashSalt"] = "TestSalt_123"
            })
            .Build();

        var hasher = new IpHasher(configuration);

        // Act
        var hash1 = hasher.HashIp("192.168.1.1");
        var hash2 = hasher.HashIp("192.168.1.2");

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HashIp_ComIpNuloOuVazio_DeveRetornarHashValidoSemExcecao(string? ipInvalido)
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var hasher = new IpHasher(configuration);

        // Act
        var hash = hasher.HashIp(ipInvalido);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Length.Should().Be(64);
    }
}
