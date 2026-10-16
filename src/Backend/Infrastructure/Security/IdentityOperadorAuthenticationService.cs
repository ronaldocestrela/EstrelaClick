using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace EstrelaClick.Backend.Infrastructure.Security;

public sealed class IdentityOperadorAuthenticationService : IOperadorAuthenticationService
{
    private readonly UserManager<IdentityUser>? _userManager;
    private readonly IConfiguration _configuration;

    public IdentityOperadorAuthenticationService(
        IConfiguration configuration,
        UserManager<IdentityUser>? userManager = null)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<Result<OperadorInfo>> ValidarCredenciaisAsync(
        string login,
        string senha,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
        {
            return Result<OperadorInfo>.Failure(AuthErrors.DadosObrigatorios);
        }

        // 1. Tentar autenticação via Identity se houver usuário cadastrado
        if (_userManager is not null)
        {
            var user = await _userManager.FindByNameAsync(login) ?? await _userManager.FindByEmailAsync(login);
            if (user is not null)
            {
                var passwordValid = await _userManager.CheckPasswordAsync(user, senha);
                if (passwordValid)
                {
                    return Result<OperadorInfo>.Success(new OperadorInfo(
                        OperadorId: user.Id,
                        Nome: user.UserName ?? login,
                        Ativo: !user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow));
                }
            }
        }

        // 2. Fallback para credenciais padrão/configuradas de bancada fabril (Dev / Fábrica)
        var defaultOpLogin = _configuration["FactorySecurity:DefaultOperator:Login"] ?? "operador";
        var defaultOpPass = _configuration["FactorySecurity:DefaultOperator:Password"] ?? "operador123";

        if (string.Equals(login, defaultOpLogin, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(senha, defaultOpPass, StringComparison.Ordinal))
        {
            return Result<OperadorInfo>.Success(new OperadorInfo(
                OperadorId: "OP-BANCADA-01",
                Nome: "Operador de Bancada 01",
                Ativo: true));
        }

        return Result<OperadorInfo>.Failure(AuthErrors.CredenciaisInvalidas);
    }
}
