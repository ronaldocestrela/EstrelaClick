using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Outbound;

public interface IQrCodeGenerator
{
    Result<byte[]> GerarPng300Dpi(string payload, int pixelsPorModulo = 20);
    Result<byte[]> GerarSvg(string payload);
}
