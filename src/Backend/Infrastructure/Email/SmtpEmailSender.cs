using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Common.Options;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Results;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EstrelaClick.Backend.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly Func<ISmtpClientWrapper> _clientFactory;

    public SmtpEmailSender(
        IOptions<SmtpOptions> options,
        ILogger<SmtpEmailSender> logger,
        Func<ISmtpClientWrapper>? clientFactory = null)
    {
        _options = options.Value;
        _logger = logger;
        _clientFactory = clientFactory ?? (() => new MailKitSmtpClientWrapper());
    }

    public async Task<Result> SendEmailAsync(EmailMessageDto message, CancellationToken cancellationToken = default)
    {
        var validationResult = ValidarParametros(message);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        using var client = _clientFactory();

        try
        {
            var mimeMessage = CriarMimeMessage(message);

            _logger.LogInformation(
                "Iniciando envio de e-mail via SMTP para {To} (Host: {Host}:{Port}, SSL: {Ssl})",
                message.To, _options.Host, _options.Port, _options.EnableSsl);

            await client.ConnectAsync(_options.Host, _options.Port, _options.EnableSsl, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("E-mail enviado com sucesso para {To}", message.To);
            return Result.Success();
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning(ex, "Falha de autenticação ao tentar enviar e-mail via SMTP para {To}", message.To);
            return Result.Failure(EmailErrors.FalhaAutenticacao);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout esgotado ao tentar comunicar com o servidor SMTP ({Host}:{Port})", _options.Host, _options.Port);
            return Result.Failure(EmailErrors.TimeoutConexao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao disparar e-mail via SMTP para {To}", message.To);
            return Result.Failure(EmailErrors.FalhaEnvio);
        }
    }

    private Result ValidarParametros(EmailMessageDto message)
    {
        if (string.IsNullOrWhiteSpace(message.To) || !message.To.Contains('@'))
        {
            return Result.Failure(EmailErrors.DestinatarioInvalido);
        }

        if (string.IsNullOrWhiteSpace(message.Subject))
        {
            return Result.Failure(EmailErrors.AssuntoObrigatorio);
        }

        if (string.IsNullOrWhiteSpace(message.BodyHtml) && string.IsNullOrWhiteSpace(message.BodyText))
        {
            return Result.Failure(EmailErrors.ConteudoObrigatorio);
        }

        if (string.IsNullOrWhiteSpace(_options.Host) || _options.Port <= 0 || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            return Result.Failure(EmailErrors.ConfiguracaoInvalida);
        }

        return Result.Success();
    }

    private MimeMessage CriarMimeMessage(EmailMessageDto message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mimeMessage.To.Add(new MailboxAddress(message.ToDisplayName ?? string.Empty, message.To));
        mimeMessage.Subject = message.Subject;

        if (message.Cc != null)
        {
            foreach (var cc in message.Cc.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                mimeMessage.Cc.Add(MailboxAddress.Parse(cc));
            }
        }

        if (message.Bcc != null)
        {
            foreach (var bcc in message.Bcc.Where(b => !string.IsNullOrWhiteSpace(b)))
            {
                mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));
            }
        }

        var builder = new BodyBuilder();
        if (!string.IsNullOrWhiteSpace(message.BodyHtml))
        {
            builder.HtmlBody = message.BodyHtml;
        }

        if (!string.IsNullOrWhiteSpace(message.BodyText))
        {
            builder.TextBody = message.BodyText;
        }

        mimeMessage.Body = builder.ToMessageBody();
        return mimeMessage;
    }
}
