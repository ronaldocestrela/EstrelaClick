namespace EstrelaClick.Backend.Infrastructure.Security;

public interface IIpHasher
{
    string HashIp(string? ipAddress);
}
