using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.UseCases;

public sealed class AutenticarOperadorUseCase : IAutenticarOperadorUseCase
{
    private readonly IOperadorAuthenticationService _operadorAuthService;
    private readonly ITokenService _tokenService;
    private static readonly TimeSpan TokenDuration = TimeSpan.FromHours(12);

    public AutenticarOperadorUseCase(
        IOperadorAuthenticationService operadorAuthService,
        ITokenService tokenService)
    {
        _operadorAuthService = operadorAuthService;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthTokenResult>> ExecutarAsync(
        AutenticarOperadorCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Login) || string.IsNullOrWhiteSpace(command.Senha))
        {
            return Result<AuthTokenResult>.Failure(AuthErrors.DadosObrigatorios);
        }

        var authResult = await _operadorAuthService.ValidarCredenciaisAsync(
            command.Login.Trim(),
            command.Senha.Trim(),
            cancellationToken);

        if (authResult.IsFailure || authResult.Value is null)
        {
            return Result<AuthTokenResult>.Failure(authResult.Error);
        }

        var operador = authResult.Value;
        if (!operador.Ativo)
        {
            return Result<AuthTokenResult>.Failure(AuthErrors.OperadorInativo);
        }

        var token = _tokenService.GerarTokenOperador(operador.OperadorId, operador.Nome, TokenDuration);
        var expiracaoUtc = DateTime.UtcNow.Add(TokenDuration);

        return Result<AuthTokenResult>.Success(new AuthTokenResult(
            Token: token,
            OperadorId: operador.OperadorId,
            Nome: operador.Nome,
            ExpiracaoUtc: expiracaoUtc));
    }
}
