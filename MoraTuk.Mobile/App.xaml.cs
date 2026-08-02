using MoraTuk.Mobile.Pages;
using MoraTuk.Mobile.Services;

namespace MoraTuk.Mobile;

public partial class App : Application
{
	public App(
    ApiService apiService,
    DriverHubService driverHubService)
    {
        InitializeComponent();

        MainPage = new NavigationPage(
            new LoginPage(
                apiService,
                driverHubService));
    }
}
