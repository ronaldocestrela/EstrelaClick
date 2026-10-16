namespace EstrelaClick.Backend.Application.Common.Options;

public sealed class QrCodeOptions
{
    public const string SectionName = "QrCode";

    public string BaseRedirecionamentoUrl { get; set; } = "https://estrela.click/r";
}
