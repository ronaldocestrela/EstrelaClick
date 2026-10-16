using EstrelaClick.Backend.Application.Common.Options;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EstrelaClick.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<QrCodeOptions>(configuration.GetSection(QrCodeOptions.SectionName));
        }

        services.AddScoped<IObterRedirecionamentoUseCase, ObterRedirecionamentoUseCase>();
        services.AddScoped<IGerarQrCodePlacaUseCase, GerarQrCodePlacaUseCase>();
        services.AddScoped<IExportarLoteQrCodeZipUseCase, ExportarLoteQrCodeZipUseCase>();
        services.AddScoped<IGerarLotePlacasUseCase, GerarLotePlacasUseCase>();
        services.AddScoped<IObterProximaPlacaPendenteUseCase, ObterProximaPlacaPendenteUseCase>();
        services.AddScoped<IConfirmarGravacaoPlacaUseCase, ConfirmarGravacaoPlacaUseCase>();
        services.AddScoped<IAutenticarOperadorUseCase, AutenticarOperadorUseCase>();
        services.AddScoped<IObterResumoLoteUseCase, ObterResumoLoteUseCase>();
        services.AddScoped<IObterMetricasFabricaUseCase, ObterMetricasFabricaUseCase>();
        services.AddScoped<IListarLotesAdminUseCase, ListarLotesAdminUseCase>();
        services.AddScoped<IConsultarAuditoriaPlacaUseCase, ConsultarAuditoriaPlacaUseCase>();
        services.AddScoped<IListarEmpresasAdminUseCase, ListarEmpresasAdminUseCase>();
        services.AddScoped<ICadastrarEmpresaAdminUseCase, CadastrarEmpresaAdminUseCase>();

        services.AddScoped<IAtivarPlacaOnboardingUseCase, AtivarPlacaOnboardingUseCase>();
        services.AddScoped<IAtualizarUrlPlacaUseCase, AtualizarUrlPlacaUseCase>();
        services.AddScoped<IConfigurarFiltroSatisfacaoUseCase, ConfigurarFiltroSatisfacaoUseCase>();
        services.AddScoped<IListarPlacasClienteUseCase, ListarPlacasClienteUseCase>();
        services.AddScoped<IObterAnalyticsClienteUseCase, ObterAnalyticsClienteUseCase>();

        return services;
    }
}
