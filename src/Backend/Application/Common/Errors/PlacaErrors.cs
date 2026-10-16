using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Common.Errors;

public static class PlacaErrors
{
    public static readonly Error SlugInvalido = new("placa.slug.invalido", "Slug deve ser um GUID sem hífens.");
    public static readonly Error NaoEncontrada = new("placa.nao_encontrada", "Placa informada não foi encontrada.");
    public static readonly Error NaoAtivada = new("placa.nao_ativada", "A placa ainda não foi vinculada e ativada.");
    public static readonly Error LoteJaExiste = new("placa.lote.duplicado", "Já existe um lote cadastrado com este número.");
    public static readonly Error LoteInvalido = new("placa.lote.invalido", "Número de lote é obrigatório e deve ter no máximo 32 caracteres.");
    public static readonly Error QuantidadeInvalida = new("placa.quantidade.invalida", "A quantidade de placas deve ser entre 1 e 10.000.");
    public static readonly Error FilaVazia = new("placa.fila.vazia", "Nenhuma placa pendente de gravação foi encontrada para os critérios informados.");
    public static readonly Error ChipUidInvalido = new("placa.chipUid.invalido", "ChipUid é obrigatório para registrar gravação.");
    public static readonly Error StatusTransicaoInvalida = new("placa.status.transicao.invalida", "A placa não está em um estado válido para esta operação.");
    public static readonly Error LoteNaoEncontrado = new("placa.lote.nao_encontrado", "O lote especificado não foi encontrado.");
}
