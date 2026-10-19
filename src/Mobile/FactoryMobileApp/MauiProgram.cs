using Microsoft.Extensions.Logging;

namespace EstrelaClick.Mobile.FactoryMobileApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

#if ANDROID
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Nfc.INfcService, Platforms.Android.Services.AndroidNfcService>();
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Mobile.ISensoryFeedbackService, Platforms.Android.Services.AndroidSensoryFeedbackService>();
#else
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Nfc.INfcService, Services.DummyNfcService>();
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Mobile.ISensoryFeedbackService, Services.DummySensoryFeedbackService>();
#endif

		// Sessão e Cliente de API de Fábrica
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Mobile.IOperatorSessionService, EstrelaClick.Shared.SharedKernel.Mobile.OperatorSessionService>();
		builder.Services.AddSingleton<EstrelaClick.Shared.SharedKernel.Mobile.IFactoryApiClient>(sp =>
		{
#if ANDROID
			// 10.0.2.2 é o alias padrão do emulador Android para o localhost da máquina hospedeira.
			// Em dispositivos físicos via USB, utilize: 'adb reverse tcp:5180 tcp:5180' ou informe o IP local do host.
			var defaultUrl = "http://10.0.2.2:5180";
#else
			var defaultUrl = "http://localhost:5180";
#endif
			var configuredUrl = Microsoft.Maui.Storage.Preferences.Default.Get("ApiBaseUrl", defaultUrl);
			var finalUrl = string.IsNullOrWhiteSpace(configuredUrl) ? defaultUrl : configuredUrl;
			var baseUri = new Uri(finalUrl);
			var httpClient = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(15) };
			return new EstrelaClick.Shared.SharedKernel.Mobile.FactoryApiClient(httpClient, sp.GetRequiredService<EstrelaClick.Shared.SharedKernel.Mobile.IOperatorSessionService>());
		});

		// ViewModels
		builder.Services.AddTransient<EstrelaClick.Shared.SharedKernel.Mobile.ViewModels.LoginViewModel>();
		builder.Services.AddTransient<EstrelaClick.Shared.SharedKernel.Mobile.ViewModels.BancadaViewModel>(sp =>
		{
			return new EstrelaClick.Shared.SharedKernel.Mobile.ViewModels.BancadaViewModel(
				sp.GetRequiredService<EstrelaClick.Shared.SharedKernel.Mobile.IFactoryApiClient>(),
				sp.GetRequiredService<EstrelaClick.Shared.SharedKernel.Mobile.IOperatorSessionService>(),
				sp.GetRequiredService<EstrelaClick.Shared.SharedKernel.Nfc.INfcService>(),
				sp.GetRequiredService<EstrelaClick.Shared.SharedKernel.Mobile.ISensoryFeedbackService>());
		});

		// Views
		builder.Services.AddTransient<Views.LoginPage>();
		builder.Services.AddTransient<Views.BancadaPage>();

		return builder.Build();
	}
}
