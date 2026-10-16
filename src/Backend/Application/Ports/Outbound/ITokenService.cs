namespace EstrelaClick.Backend.Application.Ports.Outbound;

public interface ITokenService
{
    string GerarTokenOperador(string operadorId, string nome, TimeSpan expiracao);
}
