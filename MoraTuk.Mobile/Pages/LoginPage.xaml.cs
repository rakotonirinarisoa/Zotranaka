using MoraTuk.Mobile.Services;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly DriverHubService _driverHubService;


    public LoginPage(ApiService apiService,DriverHubService driverHubService)
    {
        InitializeComponent();

        _apiService = apiService;
        _driverHubService = driverHubService;
    }


    private async void Login_Clicked(object sender, EventArgs e)
    {
        try
        {
            var dto = new LoginDto
            {
                Phone = PhoneEntry.Text ?? "",
                Password = PasswordEntry.Text ?? ""
            };


            var result = await _apiService.LoginAsync(dto);


            if (result == null)
            {
                await DisplayAlert(
                    "Erreur",
                    "Identifiants incorrects",
                    "OK");

                return;
            }


            await SecureStorage.SetAsync(
                "token",
                result.Token);


            await SessionService.SaveSession(
                result.Token,
                result.User.Id,
                result.User.Role);


            await SecureStorage.SetAsync(
                "role",
                result.User.Role);



           if(result.User.Role == "Driver")
                {
                    var driverId = await _apiService
                        .GetDriverIdAsync(result.User.Id);


                    await DisplayAlert(
                        "ID Chauffeur",
                        $"DriverId : {driverId}",
                        "OK");


                    // Application.Current.MainPage =
                    //     new NavigationPage(
                    //         new DriverHomePage(
                    //             new DriverHubService(),
                    //             driverId));
                     driverId = await _apiService.GetDriverIdAsync(result.User.Id);

                    Application.Current.MainPage =
                        new NavigationPage(
                            new DriverHomePage(
                                new DriverHubService(),
                                driverId));
                }
            else
            {
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(
                        "http://192.168.1.106:5078/")
                };


                var rideService = new RideService(
                    httpClient);


                var locationService =
                    new LocationService(httpClient);


                var distanceService =
                    new DistanceService();


                var searchService =
                    new LocationSearchService(httpClient);



                var clientPage =
                    new ClientHomePage(
                        rideService,
                        locationService,
                        distanceService,
                        searchService,
                        _apiService);



                Application.Current.MainPage =
                    new NavigationPage(clientPage);
            }

        }
        catch(Exception ex)
        {
            await DisplayAlert(
                "Erreur complète",
                ex.ToString(),
                "OK");
        }
    }
}