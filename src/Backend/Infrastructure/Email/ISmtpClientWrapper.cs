using MimeKit;

namespace EstrelaClick.Backend.Infrastructure.Email;

public interface ISmtpClientWrapper : IDisposable
{
    Task ConnectAsync(string host, int port, bool useSsl, CancellationToken cancellationToken = default);
    Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task SendAsync(MimeMessage message, CancellationToken cancellationToken = default);
    Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default);
}
