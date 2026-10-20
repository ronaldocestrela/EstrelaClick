namespace EstrelaClick.Backend.Application.Common.Options;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = false;
    public string FromEmail { get; set; } = "noreply@estrelaclick.com.br";
    public string FromName { get; set; } = "EstrelaClick";
}
