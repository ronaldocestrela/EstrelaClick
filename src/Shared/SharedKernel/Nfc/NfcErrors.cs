using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Shared.SharedKernel.Nfc;

public static class NfcErrors
{
    public static readonly Error NotSupported = new("Nfc.NotSupported", "O dispositivo móvel não possui hardware NFC disponível.");
    public static readonly Error Disabled = new("Nfc.Disabled", "O recurso NFC está desabilitado nas configurações do sistema.");
    public static readonly Error TagLost = new("Nfc.TagLost", "A tag NFC foi afastada antes da conclusão da operação.");
    public static readonly Error TagReadOnly = new("Nfc.TagReadOnly", "A tag NFC está configurada como somente leitura e não pode ser gravada.");
    public static readonly Error CapacityExceeded = new("Nfc.CapacityExceeded", "O payload excede a capacidade de memória utilizável da tag.");
    public static readonly Error InvalidUri = new("Nfc.InvalidUri", "A URL informada para gravação no chip é inválida.");
    public static readonly Error InvalidUid = new("Nfc.InvalidUid", "O UID da tag NFC é inválido ou não pôde ser lido.");
    public static readonly Error WriteFailed = new("Nfc.WriteFailed", "Falha de entrada/saída durante a indução magnética de gravação NDEF.");
    public static readonly Error NoTagDetected = new("Nfc.NoTagDetected", "Nenhuma tag NFC foi detectada no campo de indução.");
}
