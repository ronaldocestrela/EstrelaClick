using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace EstrelaClick.Backend.Infrastructure.Security;

public sealed class IpHasher : IIpHasher
{
    private readonly byte[] _saltBytes;

    public IpHasher(IConfiguration configuration)
    {
        var salt = configuration["Security:IpHashSalt"] ?? "EstrelaClick_Default_Salt_2026";
        _saltBytes = Encoding.UTF8.GetBytes(salt);
    }

    public string HashIp(string? ipAddress)
    {
        var ip = string.IsNullOrWhiteSpace(ipAddress) ? "0.0.0.0" : ipAddress.Trim();
        var ipBytes = Encoding.UTF8.GetBytes(ip);

        Span<byte> combined = stackalloc byte[ipBytes.Length + _saltBytes.Length];
        ipBytes.CopyTo(combined);
        _saltBytes.CopyTo(combined[ipBytes.Length..]);

        Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(combined, hashBytes);

        return Convert.ToHexStringLower(hashBytes);
    }
}
