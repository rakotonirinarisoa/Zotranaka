using MoraTuk.Mobile.Helpers;
using MoraTuk.Mobile.Pages;
using MoraTuk.Mobile.Services;

namespace MoraTuk.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Page temporaire pendant le chargement
        MainPage = new ContentPage
        {
            Content = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 15,

                Children =
                {
                    new ActivityIndicator
                    {
                        IsRunning = true,
                        WidthRequest = 50,
                        HeightRequest = 50
                    },

                    new Label
                    {
                        Text = "Connexion au serveur...",
                        FontSize = 18,
                        HorizontalTextAlignment =
                            TextAlignment.Center
                    }
                }
            }
        };

        // Charger la configuration
        _ = InitializeApplicationAsync();
    }

    private async Task InitializeApplicationAsync()
    {
        try
        {
            // ========================================================
            // 1. CHARGER CONFIG.JSON DEPUIS GITHUB
            // ========================================================

            var configService =
                new ConfigService();

            var loaded =
                await configService.LoadConfigAsync();

            if (!loaded)
            {
                await ShowError(
                    "Impossible de charger la configuration.\n\n" +
                    "Vérifie la connexion Internet et le fichier " +
                    "config.json sur GitHub.");

                return;
            }

            // ========================================================
            // 2. VÉRIFIER L'URL API
            // ========================================================

            if (!ApiSettings.IsConfigured)
            {
                await ShowError(
                    "L'URL de l'API n'est pas configurée.");

                return;
            }

            Console.WriteLine(
                $"API utilisée : {ApiSettings.BaseUrl}");

            // ========================================================
            // 3. CRÉER LES SERVICES
            // ========================================================

            var apiService =
                new ApiService();

            var driverHubService =
                new DriverHubService();

            var rideService =
                new RideService();

            var locationService =
                new LocationService();

            var distanceService =
                new DistanceService();

            var locationSearchService =
                new LocationSearchService();

            // ========================================================
            // 4. CRÉER LOGIN PAGE
            // ========================================================

            var loginPage =
                new LoginPage(
                    apiService,
                    driverHubService,
                    rideService,
                    locationService,
                    distanceService,
                    locationSearchService);

            // ========================================================
            // 5. AFFICHER LOGIN
            // ========================================================

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage =
                    new NavigationPage(
                        loginPage);
            });

            Console.WriteLine(
                "Application initialisée avec succès.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "ERREUR INITIALISATION APPLICATION");

            Console.WriteLine(
                ex.ToString());

            await ShowError(
                "Erreur lors du démarrage de l'application.\n\n" +
                ex.Message);
        }
    }

    private async Task ShowError(string message)
    {
        await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                if (MainPage != null)
                {
                    await MainPage.DisplayAlert(
                        "Erreur",
                        message,
                        "OK");
                }
            });
    }
}