using MoraTuk.Mobile.Services;
using MoraTuk.Mobile.Models;
namespace MoraTuk.Mobile.Pages;

public partial class DriverHomePage : ContentPage
{
    private readonly DriverHubService _hubService;
    private readonly int _driverId;
     private RideNotification? _currentRide;
    public DriverHomePage(DriverHubService hubService,
        int driverId)
    {
        InitializeComponent();
        _hubService = hubService;
        _driverId = driverId;
        _hubService.OnNewRideReceived = ride =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                
                _currentRide = ride;

                RideInfoLabel.Text =
                            $"""
                            🚕 Course #{ride.RideId}

                            📏 Distance : {ride.DistanceToDriver:F2} km

                            👥 Passagers : {ride.Passengers}

                            🚖 Type : {ride.RideType}

                            💰 Prix : {ride.Price} Ar

                            📍 Départ
                            {ride.PickupLatitude:F6}, {ride.PickupLongitude:F6}

                            🎯 Destination
                            {ride.DestinationLatitude:F6}, {ride.DestinationLongitude:F6}
                            """;
                RideFrame.IsVisible = true;
            });
        };
    }
    private bool _started = false;
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if(_started)
        return;

         _started = true;
        try
        {
            await _hubService.StartAsync(_driverId);
            StatusLabel.Text = "Statut : En ligne 🟢";

            await DisplayAlert(
                "SignalR",
                "Chauffeur connecté au serveur",
                "OK");
        }
        catch(Exception ex)
        {
            await DisplayAlert(
                "SignalR erreur",
                ex.Message,
                "OK");
        }
    }
    private async void AcceptRide_Clicked(object sender, EventArgs e)
    {
        if (_currentRide == null)
            return;


        await DisplayAlert(
            "Course acceptée",
            $"Course #{_currentRide.RideId} acceptée",
            "OK");


        RideTitleLabel.Text = "🚕 Course acceptée";

        RideStatusLabel.Text =
            "🟢 En route vers le client";


        AcceptButton.IsVisible = false;
        RejectButton.IsVisible = false;
    }
    private async void RejectRide_Clicked(object sender, EventArgs e)
    {
        _currentRide = null;

        RideFrame.IsVisible = false;

        await DisplayAlert(
            "Course",
            "Course refusée",
            "OK");
    }
}