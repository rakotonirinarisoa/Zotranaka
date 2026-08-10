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
                fonts.AddFont(
                    "OpenSans-Regular.ttf",
                    "OpenSansRegular");

                fonts.AddFont(
                    "OpenSans-Semibold.ttf",
                    "OpenSansSemibold");
            });

        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<ConfigService>();
        builder.Services.AddSingleton<LocationSearchService>();
        builder.Services.AddSingleton<DriverHubService>();

        builder.Services.AddSingleton<RideService>();
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<DistanceService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}