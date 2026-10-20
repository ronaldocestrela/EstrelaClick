using System.Net.Sockets;
using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Common.Options;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Infrastructure.Email;
using FluentAssertions;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace EstrelaClick.Tests.UnitTests.Infrastructure.Email;

public class SmtpEmailSenderTests
{
    private readonly ISmtpClientWrapper _smtpClient = Substitute.For<ISmtpClientWrapper>();
    private readonly ILogger<SmtpEmailSender> _logger = Substitute.For<ILogger<SmtpEmailSender>>();
    private readonly SmtpOptions _options = new()
    {
        Host = "smtp.exemplo.com",
        Port = 587,
        Username = "usuario@exemplo.com",
        Password = "senha_secreta",
        EnableSsl = true,
        FromEmail = "noreply@estrelaclick.com.br",
        FromName = "EstrelaClick"
    };

    private SmtpEmailSender CreateSut(SmtpOptions? customOptions = null)
    {
        var optionsWrapper = Substitute.For<IOptions<SmtpOptions>>();
        optionsWrapper.Value.Returns(customOptions ?? _options);
        return new SmtpEmailSender(optionsWrapper, _logger, () => _smtpClient);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("email-invalido-sem-arroba")]
    public async Task SendEmailAsync_DeveRetornarDestinatarioInvalido_QuandoEmailForInvalido(string? destinatarioInvalido)
    {
        // Arrange
        var sut = CreateSut();
        var message = new EmailMessageDto(
            To: destinatarioInvalido!,
            Subject: "Assunto Válido",
            BodyHtml: "<p>Olá</p>");

        // Act
        var result = await sut.SendEmailAsync(message);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.DestinatarioInvalido);
        await _smtpClient.DidNotReceiveWithAnyArgs().ConnectAsync(default!, default, default, default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendEmailAsync_DeveRetornarAssuntoObrigatorio_QuandoAssuntoForVazio(string? assuntoInvalido)
    {
        // Arrange
        var sut = CreateSut();
        var message = new EmailMessageDto(
            To: "destinatario@teste.com",
            Subject: assuntoInvalido!,
            BodyHtml: "<p>Olá</p>");

        // Act
        var result = await sut.SendEmailAsync(message);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.AssuntoObrigatorio);
        await _smtpClient.DidNotReceiveWithAnyArgs().ConnectAsync(default!, default, default, default);
    }

    [Fact]
    public async Task SendEmailAsync_DeveRetornarConteudoObrigatorio_QuandoAmbosCorposForemVazios()
    {
        // Arrange
        var sut = CreateSut();
        var message = new EmailMessageDto(
            To: "destinatario@teste.com",
            Subject: "Assunto",
            BodyHtml: null,
            BodyText: null);

        // Act
        var result = await sut.SendEmailAsync(message);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.ConteudoObrigatorio);
        await _smtpClient.DidNotReceiveWithAnyArgs().ConnectAsync(default!, default, default, default);
    }

    [Theory]
    [InlineData("", 587, "noreply@estrelaclick.com.br")]
    [InlineData("smtp.teste.com", 0, "noreply@estrelaclick.com.br")]
    [InlineData("smtp.teste.com", -1, "noreply@estrelaclick.com.br")]
    [InlineData("smtp.teste.com", 587, "")]
    public async Task SendEmailAsync_DeveRetornarConfiguracaoInvalida_QuandoOpcoesForemInvalidas(
        string host, int port, string fromEmail)
    {
        // Arrange
        var invalidOptions = new SmtpOptions
        {
            Host = host,
            Port = port,
            FromEmail = fromEmail
        };
        var sut = CreateSut(invalidOptions);
        var message = new EmailMessageDto("cliente@teste.com", "Assunto", BodyText: "Texto");

        // Act
        var result = await sut.SendEmailAsync(message);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.ConfiguracaoInvalida);
        await _smtpClient.DidNotReceiveWithAnyArgs().ConnectAsync(default!, default, default, default);
    }

    [Fact]
    public async Task SendEmailAsync_DeveEnviarComSucesso_QuandoDadosEConfiguracoesValidosComAutenticacao()
    {
        // Arrange
        var sut = CreateSut();
        var message = new EmailMessageDto(
            To: "cliente@teste.com",
            Subject: "Boas-vindas ao EstrelaClick",
            BodyHtml: "<h1>Bem-vindo!</h1>",
            BodyText: "Bem-vindo!",
            ToDisplayName: "Cliente Teste",
            Cc: new[] { "copia@teste.com" });

        // Act
        var result = await sut.SendEmailAsync(message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _smtpClient.Received(1).ConnectAsync(_options.Host, _options.Port, _options.EnableSsl, Arg.Any<CancellationToken>());
        await _smtpClient.Received(1).AuthenticateAsync(_options.Username!, _options.Password!, Arg.Any<CancellationToken>());
        await _smtpClient.Received(1).SendAsync(Arg.Is<MimeMessage>(m =>
            m.Subject == message.Subject &&
            m.To.Count == 1 &&
            m.Cc.Count == 1), Arg.Any<CancellationToken>());
        await _smtpClient.Received(1).DisconnectAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailAsync_DeveNaoAutenticar_QuandoCredenciaisNaoForemConfiguradas()
    {
        // Arrange
        var optionsSemAuth = new SmtpOptions
        {
            Host = "localhost",
            Port = 1025,
            Username = null,
            Password = null,
            EnableSsl = false,
            FromEmail = "noreply@estrelaclick.com.br",
            FromName = "EstrelaClick"
        };
        var sut = CreateSut(optionsSemAuth);
        var message = new EmailMessageDto("cliente@teste.com", "Teste Local", BodyText: "Texto de teste");

        // Act
        var result = await sut.SendEmailAsync(message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _smtpClient.Received(1).ConnectAsync("localhost", 1025, false, Arg.Any<CancellationToken>());
        await _smtpClient.DidNotReceiveWithAnyArgs().AuthenticateAsync(default!, default!, default);
        await _smtpClient.Received(1).SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>());
        await _smtpClient.Received(1).DisconnectAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailAsync_DeveRetornarFalhaAutenticacao_QuandoServidorRejeitarCredenciais()
    {
        // Arrange
        var sut = CreateSut();
        var message = new EmailMessageDto("cliente@teste.com", "Assunto", BodyText: "Texto");

        _smtpClient
            .AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AuthenticationException("Invalid credentials"));

        // Act
        var result = await sut.SendEmailAsync(message);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.FalhaAutenticacao);
    }

    [Fact]
    public async Task SendEmailAsync_DeveRetornarTimeoutConexao_QuandoOcorrerTimeout()
    {
        // Arrange
        var sut = CreateSut();
        var message = new EmailMessageDto("cliente@teste.com", "Assunto", BodyText: "Texto");

        _smtpClient
            .ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Timed out connecting to SMTP host"));

        // Act
        var result = await sut.SendEmailAsync(message);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.TimeoutConexao);
    }

    [Fact]
    public async Task SendEmailAsync_DeveRetornarFalhaEnvio_QuandoOcorrerExcecaoDeSocket()
    {
        // Arrange
        var sut = CreateSut();
        var message = new EmailMessageDto("cliente@teste.com", "Assunto", BodyText: "Texto");

        _smtpClient
            .ConnectAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SocketException((int)SocketError.ConnectionRefused));

        // Act
        var result = await sut.SendEmailAsync(message);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmailErrors.FalhaEnvio);
    }
}
