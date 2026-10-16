using EstrelaClick.Backend.Application.Ports.Outbound;

namespace EstrelaClick.Backend.Infrastructure.Security;

public sealed class GuidSlugGenerator : ISlugGenerator
{
    public string GerarSlug()
    {
        return Guid.NewGuid().ToString("N");
    }
}
