using BaralhoPoliglota.Core.Persistencia;
using BaralhoPoliglota.Core.Services;
using BaralhoPoliglota.ViewModels;
using BaralhoPoliglota.Views;
using Microsoft.Extensions.Logging;

namespace BaralhoPoliglota;

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

		RegistrarServicos(builder.Services);
		RegistrarTelas(builder.Services);

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void RegistrarServicos(IServiceCollection servicos)
	{
		servicos.AddSingleton(_ =>
		{
			var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
			http.DefaultRequestHeaders.UserAgent.ParseAdd("BaralhoPoliglota/1.0");
			return http;
		});

		servicos.AddSingleton<ITraducaoService, TraducaoService>();
		servicos.AddSingleton<IDicionarioService, DicionarioService>();
		servicos.AddSingleton<IBaralhoService, BaralhoService>();

		servicos.AddSingleton<IPalavraRepository>(_ => new PalavraRepository(FileSystem.AppDataDirectory));
		servicos.AddSingleton<IPartidaRepository>(_ => new PartidaRepository(FileSystem.AppDataDirectory));
	}

	private static void RegistrarTelas(IServiceCollection servicos)
	{
		servicos.AddTransient<TraduzirViewModel>();
		servicos.AddTransient<MeuBaralhoViewModel>();
		servicos.AddSingleton<JogarViewModel>();
		servicos.AddTransient<DetalhePalavraViewModel>();
		servicos.AddTransient<ResultadoViewModel>();
		servicos.AddTransient<PlacarViewModel>();

		servicos.AddTransient<TraduzirPage>();
		servicos.AddTransient<MeuBaralhoPage>();
		servicos.AddSingleton<JogarPage>();
		servicos.AddTransient<DetalhePalavraPage>();
		servicos.AddTransient<ResultadoPage>();
		servicos.AddTransient<PlacarPage>();
		servicos.AddTransient<SobrePage>();
	}
}
