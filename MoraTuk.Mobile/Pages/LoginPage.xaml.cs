using MoraTuk.Mobile.Helpers;
using MoraTuk.Mobile.Models;
using MoraTuk.Mobile.Services;

namespace MoraTuk.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly DriverHubService _driverHubService;
    private readonly RideService _rideService;
    private readonly LocationService _locationService;
    private readonly DistanceService _distanceService;
    private readonly LocationSearchService _searchService;

    public LoginPage(
        ApiService apiService,
        DriverHubService driverHubService,
        RideService rideService,
        LocationService locationService,
        DistanceService distanceService,
        LocationSearchService searchService)
    {
        InitializeComponent();

        _apiService = apiService;
        _driverHubService = driverHubService;
        _rideService = rideService;
        _locationService = locationService;
        _distanceService = distanceService;
        _searchService = searchService;
    }

    // ============================================================
    // LOGIN
    // ============================================================

    private async void Login_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            // ====================================================
            // VÉRIFICATION URL CENTRALE
            // ====================================================

            if (!ApiSettings.IsConfigured)
            {
                await DisplayAlert(
                    "Configuration",
                    "ApiSettings.BaseUrl n'est pas configuré.",
                    "OK");

                return;
            }

            await DisplayAlert(
                "API",
                $"URL utilisée :\n\n{ApiSettings.BaseUrl}",
                "OK");

            // ====================================================
            // DONNÉES LOGIN
            // ====================================================

            var dto = new LoginDto
            {
                Phone =
                    PhoneEntry.Text ?? "",

                Password =
                    PasswordEntry.Text ?? ""
            };

            // ====================================================
            // LOGIN API
            // ====================================================

            var result =
                await _apiService.LoginAsync(dto);

            if (result == null)
            {
                await DisplayAlert(
                    "Erreur",
                    "Identifiants incorrects.",
                    "OK");

                return;
            }

            // ====================================================
            // SAUVEGARDE SESSION
            // ====================================================

            await SecureStorage.SetAsync(
                "token",
                result.Token);

            await SecureStorage.SetAsync(
                "userId",
                result.User.Id.ToString());

            await SecureStorage.SetAsync(
                "role",
                result.User.Role);

            await SessionService.SaveSession(
                result.Token,
                result.User.Id,
                result.User.Role);

            // ====================================================
            // OWNER / PROPRIÉTAIRE
            // ====================================================

            if (result.User.Role == "Owner")
            {
                await DisplayAlert(
                    "Connexion propriétaire",
                    $"UserId : {result.User.Id}\n" +
                    $"Role : {result.User.Role}",
                    "OK");

                Application.Current!.MainPage =
                    new NavigationPage(
                        new OwnerHomePage(
                            _apiService));

                return;
            }

            // ====================================================
            // DRIVER
            // ====================================================

            if (result.User.Role == "Driver")
            {
                await DisplayAlert(
                    "Connexion chauffeur",
                    $"UserId : {result.User.Id}\n" +
                    $"Role : {result.User.Role}",
                    "OK");

                var driverId =
                    await _apiService.GetDriverIdAsync(
                        result.User.Id);

                await DisplayAlert(
                    "Chauffeur",
                    $"DriverId : {driverId}\n\n" +
                    $"API :\n{ApiSettings.BaseUrl}",
                    "OK");

                Application.Current!.MainPage =
                    new NavigationPage(
                        new DriverHomePage(
                            _driverHubService,
                            driverId));

                return;
            }

            // ====================================================
            // CLIENT
            // ====================================================

            if (result.User.Role == "Client")
            {
                await DisplayAlert(
                    "Connexion client",
                    $"UserId : {result.User.Id}\n" +
                    $"Role : {result.User.Role}\n\n" +
                    $"API :\n{ApiSettings.BaseUrl}",
                    "OK");

                var clientPage =
                    new ClientHomePage(
                        _rideService,
                        _locationService,
                        _distanceService,
                        _searchService,
                        _apiService);

                Application.Current!.MainPage =
                    new NavigationPage(
                        clientPage);

                return;
            }

            // ====================================================
            // RÔLE INCONNU
            // ====================================================

            await DisplayAlert(
                "Erreur",
                $"Rôle utilisateur inconnu : {result.User.Role}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erreur complète",
                ex.ToString(),
                "OK");
        }
    }
}