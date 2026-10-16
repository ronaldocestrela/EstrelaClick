using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Shared.SharedKernel.Mobile;

public static class MobileErrors
{
    public static readonly Error CredenciaisInvalidas = new("mobile.auth.credenciais_invalidas", "Login ou senha inválidos.");
    public static readonly Error DadosObrigatorios = new("mobile.auth.dados_obrigatorios", "Login e senha são obrigatórios.");
    public static readonly Error NaoAutenticado = new("mobile.auth.nao_autenticado", "Nenhum operador autenticado.");
    public static readonly Error SessaoExpirada = new("mobile.auth.sessao_expirada", "A sessão do operador expirou.");
    public static readonly Error FalhaComunicacao = new("mobile.api.falha_comunicacao", "Falha de comunicação com o servidor da fábrica.");
    public static readonly Error NaoAutorizado = new("mobile.api.nao_autorizado", "Acesso não autorizado ou sessão inválida.");
    public static readonly Error FilaVazia = new("mobile.api.fila_vazia", "Nenhuma placa pendente encontrada na fila.");
    public static readonly Error RespostaInvalida = new("mobile.api.resposta_invalida", "Resposta inesperada ou formato inválido retornado pelo servidor.");
    public static readonly Error SemPlacaAlocada = new("mobile.bancada.sem_placa_alocada", "Nenhuma placa pendente alocada na bancada para gravação.");
    public static readonly Error GravacaoEmAndamento = new("mobile.bancada.gravacao_em_andamento", "Já existe uma operação de gravação em andamento.");
}
