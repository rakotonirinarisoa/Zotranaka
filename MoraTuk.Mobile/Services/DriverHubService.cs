using Microsoft.AspNetCore.SignalR.Client;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class DriverHubService
{
    private HubConnection? _connection;
    public Action<RideNotification>? OnNewRideReceived { get; set; }


    public async Task StartAsync(int driverId)
    {
        _connection =
            new HubConnectionBuilder()
            .WithUrl(
                "http://192.168.1.106:5078/trackingHub")
            .WithAutomaticReconnect()
            .Build();


      _connection.On<RideNotification>(
            "NewRide",
            ride =>
            {
                try
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "TEST",
                                "NewRide est arrivé dans le mobile",
                                "OK");


                            await Application.Current.MainPage.DisplayAlert(
                                "SignalR reçu",
                                $"Nouvelle course ID : {ride.RideId}\nPrix : {ride.Price} Ar",
                                "OK");


                            OnNewRideReceived?.Invoke(ride);
                        }
                        catch (Exception ex)
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "Erreur affichage NewRide",
                                ex.Message,
                                "OK");
                        }
                    });
                }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Erreur SignalR NewRide",
                            ex.Message,
                            "OK");
                    });
                }
            });


        await _connection.StartAsync();

        await Application.Current.MainPage.DisplayAlert(
            "SignalR",
            "Connexion ouverte",
            "OK");

        await _connection.InvokeAsync(
            "RegisterDriver",
            driverId);

        await Application.Current.MainPage.DisplayAlert(
            "SignalR",
            "Connexion établie",
            "OK");

        Console.WriteLine(
        $"Chauffeur {driverId} prêt à recevoir les courses");
    }
}