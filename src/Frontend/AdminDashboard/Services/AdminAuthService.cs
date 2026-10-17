using System.Security.Cryptography;
using System.Text;
using EstrelaClick.Shared.SharedKernel.Admin;
using EstrelaClick.Shared.SharedKernel.Results;
using Microsoft.Extensions.Options;

namespace EstrelaClick.Frontend.AdminDashboard.Services;

public interface IAdminAuthService
{
    Result<AdminAuthSuccessDto> ValidarCredenciais(AdminLoginRequest request);
}

public sealed class AdminAuthService : IAdminAuthService
{
    private readonly AdminSecurityOptions _options;

    public AdminAuthService(IOptions<AdminSecurityOptions> options)
    {
        _options = options.Value;
    }

    public Result<AdminAuthSuccessDto> ValidarCredenciais(AdminLoginRequest request)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Usuario) ||
            string.IsNullOrWhiteSpace(request.Senha))
        {
            return Result<AdminAuthSuccessDto>.Failure(new Error(
                "admin.auth.campos_obrigatorios",
                "Informe o e-mail e a senha para acessar o painel."
            ));
        }

        if (string.IsNullOrWhiteSpace(_options.MasterUser) ||
            string.IsNullOrWhiteSpace(_options.MasterPassword))
        {
            return Result<AdminAuthSuccessDto>.Failure(new Error(
                "admin.auth.configuracao_invalida",
                "Credenciais de administrador master não configuradas no ambiente."
            ));
        }

        var userNormalized = request.Usuario.Trim().ToLowerInvariant();
        var masterUserNormalized = _options.MasterUser.Trim().ToLowerInvariant();

        var userMatch = CompararHashTempoConstante(userNormalized, masterUserNormalized);
        var passwordMatch = CompararHashTempoConstante(request.Senha, _options.MasterPassword);

        if (!userMatch || !passwordMatch)
        {
            return Result<AdminAuthSuccessDto>.Failure(new Error(
                "admin.auth.credenciais_invalidas",
                "Usuário ou senha incorretos."
            ));
        }

        return Result<AdminAuthSuccessDto>.Success(new AdminAuthSuccessDto(
            masterUserNormalized,
            "MasterAdmin",
            DateTime.UtcNow
        ));
    }

    private static bool CompararHashTempoConstante(string a, string b)
    {
        var hashA = SHA256.HashData(Encoding.UTF8.GetBytes(a));
        var hashB = SHA256.HashData(Encoding.UTF8.GetBytes(b));
        return CryptographicOperations.FixedTimeEquals(hashA, hashB);
    }
}
