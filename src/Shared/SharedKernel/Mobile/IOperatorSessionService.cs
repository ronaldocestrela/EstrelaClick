namespace EstrelaClick.Shared.SharedKernel.Mobile;

public interface IOperatorSessionService
{
    bool IsAuthenticated { get; }
    string? OperadorId { get; }
    string? OperadorNome { get; }
    string? Token { get; }
    DateTime? ExpiracaoUtc { get; }
    int QuantidadeGravadaSessao { get; }

    void IniciarSessao(string operadorId, string operadorNome, string token, DateTime expiracaoUtc);
    void IncrementarGravadas();
    void EncerrarSessao();
}
