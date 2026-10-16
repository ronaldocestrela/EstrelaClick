namespace EstrelaClick.Shared.SharedKernel.Mobile;

public sealed class OperatorSessionService : IOperatorSessionService
{
    private readonly object _lock = new();

    public bool IsAuthenticated { get; private set; }
    public string? OperadorId { get; private set; }
    public string? OperadorNome { get; private set; }
    public string? Token { get; private set; }
    public DateTime? ExpiracaoUtc { get; private set; }
    public int QuantidadeGravadaSessao { get; private set; }

    public void IniciarSessao(string operadorId, string operadorNome, string token, DateTime expiracaoUtc)
    {
        lock (_lock)
        {
            OperadorId = operadorId;
            OperadorNome = operadorNome;
            Token = token;
            ExpiracaoUtc = expiracaoUtc;
            IsAuthenticated = true;
            QuantidadeGravadaSessao = 0;
        }
    }

    public void IncrementarGravadas()
    {
        lock (_lock)
        {
            QuantidadeGravadaSessao++;
        }
    }

    public void EncerrarSessao()
    {
        lock (_lock)
        {
            OperadorId = null;
            OperadorNome = null;
            Token = null;
            ExpiracaoUtc = null;
            IsAuthenticated = false;
            QuantidadeGravadaSessao = 0;
        }
    }
}
