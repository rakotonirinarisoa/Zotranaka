using Microsoft.Extensions.Logging;
using MoraTuk.Mobile.Services;

namespace MoraTuk.Mobile;

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


            // Connexion API MoraTuk
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://192.168.1.106:5078/")
            };

            builder.Services.AddSingleton(httpClient);

            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<LocationSearchService>();
            builder.Services.AddSingleton<DriverHubService>();


#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}