using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EstrelaClick.Backend.Infrastructure.Email;

public class MailKitSmtpClientWrapper : ISmtpClientWrapper
{
    private readonly SmtpClient _client = new();
    private bool _disposed;

    public async Task ConnectAsync(string host, int port, bool useSsl, CancellationToken cancellationToken = default)
    {
        var secureSocketOptions = useSsl
            ? (port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
            : SecureSocketOptions.None;

        await _client.ConnectAsync(host, port, secureSocketOptions, cancellationToken);
    }

    public async Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        await _client.AuthenticateAsync(username, password, cancellationToken);
    }

    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken = default)
    {
        await _client.SendAsync(message, cancellationToken);
    }

    public async Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default)
    {
        if (_client.IsConnected)
        {
            await _client.DisconnectAsync(quit, cancellationToken);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
