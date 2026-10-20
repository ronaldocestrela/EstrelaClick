using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Common.Errors;

public static class EmailErrors
{
    public static readonly Error DestinatarioInvalido = new("email.destinatario.invalido", "O endereço de e-mail do destinatário é obrigatório e deve ser válido.");
    public static readonly Error AssuntoObrigatorio = new("email.assunto.obrigatorio", "O assunto do e-mail é obrigatório.");
    public static readonly Error ConteudoObrigatorio = new("email.conteudo.obrigatorio", "O e-mail deve conter corpo em formato HTML ou texto puro.");
    public static readonly Error ConfiguracaoInvalida = new("email.configuracao.invalida", "Configurações do servidor SMTP incompletas ou inválidas.");
    public static readonly Error FalhaAutenticacao = new("email.falha.autenticacao", "Falha de autenticação ao conectar no servidor SMTP.");
    public static readonly Error TimeoutConexao = new("email.timeout.conexao", "Tempo limite de conexão esgotado ao contatar o servidor SMTP.");
    public static readonly Error FalhaEnvio = new("email.falha.envio", "Ocorreu um erro inesperado durante o disparo do e-mail via SMTP.");
}
