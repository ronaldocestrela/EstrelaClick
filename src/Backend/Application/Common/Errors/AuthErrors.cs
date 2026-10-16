using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Common.Errors;

public static class AuthErrors
{
    public static readonly Error CredenciaisInvalidas = new("auth.credenciais.invalidas", "Login ou senha de operador incorretos.");
    public static readonly Error DadosObrigatorios = new("auth.dados_obrigatorios", "Login e senha são obrigatórios.");
    public static readonly Error OperadorInativo = new("auth.operador.inativo", "Operador inativo ou não autorizado para bancada.");
}
