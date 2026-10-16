using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EstrelaClick.Backend.Application.Ports.Outbound;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EstrelaClick.Backend.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GerarTokenOperador(string operadorId, string nome, TimeSpan expiracao)
    {
        var jwtSecret = _configuration["Jwt:SecretKey"] ?? "chave-super-secreta-padrao-jwt-bancada-2026-estrelaclick";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, operadorId),
            new Claim(ClaimTypes.Name, nome),
            new Claim(ClaimTypes.Role, "Operador"),
            new Claim("matricula", operadorId)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(expiracao),
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(securityToken);
    }
}
