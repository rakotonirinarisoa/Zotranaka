using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using MoraTuk.Mobile.Helpers;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class DriverHubService
{
    private HubConnection? _connection;

    // ============================================================
    // ÉVÉNEMENT NOUVELLE COURSE
    // ============================================================

    public Action<RideNotification>? OnNewRideReceived { get; set; }

    private int _driverId;

    // ============================================================
    // START SIGNALR
    // ============================================================

    public async Task StartAsync(int driverId)
    {
        if (driverId <= 0)
            throw new ArgumentException(
                $"DriverId invalide : {driverId}");

        if (!ApiSettings.IsConfigured)
            throw new Exception(
                "ApiSettings.BaseUrl n'est pas configuré.");

        _driverId = driverId;

        // --------------------------------------------------------
        // SI DÉJÀ CONNECTÉ
        // --------------------------------------------------------

        if (_connection != null &&
            _connection.State == HubConnectionState.Connected)
        {
            Console.WriteLine(
                $"SignalR déjà connecté pour DriverId={driverId}");

            // On s'assure quand même que le chauffeur
            // est enregistré.
            await RegisterDriverAsync();

            return;
        }

        // --------------------------------------------------------
        // NETTOYER ANCIENNE CONNEXION
        // --------------------------------------------------------

        if (_connection != null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch
            {
                // Ignorer
            }

            _connection = null;
        }

        // --------------------------------------------------------
        // URL SIGNALR
        // --------------------------------------------------------

        var hubUrl =
            $"{ApiSettings.BaseUrl.TrimEnd('/')}/trackingHub";

        Console.WriteLine(
            "==========================================");

        Console.WriteLine(
            "SIGNALR START");

        Console.WriteLine(
            $"BaseUrl : {ApiSettings.BaseUrl}");

        Console.WriteLine(
            $"HubUrl : {hubUrl}");

        Console.WriteLine(
            $"DriverId : {driverId}");

        Console.WriteLine(
            "==========================================");

        // --------------------------------------------------------
        // CREATION CONNEXION
        // --------------------------------------------------------

        _connection =
            new HubConnectionBuilder()
                .WithUrl(
                    hubUrl,
                    options =>
                    {
                        // Cloudflare + téléphone :
                        // LongPolling est plus fiable pour nos tests.
                        options.Transports =
                            HttpTransportType.LongPolling;
                    })
                .WithAutomaticReconnect(
                    new[]
                    {
                        TimeSpan.Zero,
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10)
                    })
                .Build();

        // ========================================================
        // NOUVELLE COURSE
        // ========================================================

        _connection.On<RideNotification>(
            "NewRide",
            async ride =>
            {
                try
                {
                    Console.WriteLine(
                        "==========================================");

                    Console.WriteLine(
                        "🚕 NOUVELLE COURSE SIGNALR");

                    Console.WriteLine(
                        $"RideId : {ride.RideId}");

                    Console.WriteLine(
                        $"DriverId : {ride.DriverId}");

                    Console.WriteLine(
                        $"Status : {ride.Status}");

                    Console.WriteLine(
                        $"Departure : {ride.Departure}");

                    Console.WriteLine(
                        $"Destination : {ride.Destination}");

                    Console.WriteLine(
                        $"Price : {ride.Price}");

                    Console.WriteLine(
                        "==========================================");

                    // ------------------------------------------------
                    // IMPORTANT :
                    // envoyer directement à DriverHomePage
                    // ------------------------------------------------

                    if (OnNewRideReceived == null)
                    {
                        Console.WriteLine(
                            "⚠️ OnNewRideReceived est NULL.");

                        return;
                    }

                    await MainThread.InvokeOnMainThreadAsync(
                        () =>
                        {
                            try
                            {
                                OnNewRideReceived.Invoke(ride);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(
                                    $"ERREUR CALLBACK NEW RIDE : {ex}");
                            }
                        });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"ERREUR SIGNALR NEW RIDE : {ex}");
                }
            });

        // ========================================================
        // RECONNEXION
        // ========================================================

        _connection.Reconnecting +=
            error =>
            {
                Console.WriteLine(
                    "==========================================");

                Console.WriteLine(
                    "⚠️ SIGNALR RECONNECTING");

                Console.WriteLine(
                    error?.ToString()
                    ?? "Erreur inconnue");

                Console.WriteLine(
                    "==========================================");

                return Task.CompletedTask;
            };

        // ========================================================
        // RECONNECTÉ
        // ========================================================

        _connection.Reconnected +=
            async connectionId =>
            {
                Console.WriteLine(
                    "==========================================");

                Console.WriteLine(
                    "✅ SIGNALR RECONNECTED");

                Console.WriteLine(
                    $"ConnectionId : {connectionId}");

                Console.WriteLine(
                    $"DriverId : {_driverId}");

                Console.WriteLine(
                    "==========================================");

                try
                {
                    await RegisterDriverAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"ERREUR REGISTER RECONNECT : {ex}");
                }
            };

        // ========================================================
        // FERMETURE
        // ========================================================

        _connection.Closed +=
            error =>
            {
                Console.WriteLine(
                    "==========================================");

                Console.WriteLine(
                    "❌ SIGNALR CLOSED");

                Console.WriteLine(
                    error?.ToString()
                    ?? "Connexion fermée.");

                Console.WriteLine(
                    "==========================================");

                return Task.CompletedTask;
            };

        // ========================================================
        // START
        // ========================================================

        Console.WriteLine(
            "SIGNALR : StartAsync()...");

        await _connection.StartAsync();

        Console.WriteLine(
            "==========================================");

        Console.WriteLine(
            "✅ SIGNALR CONNECTÉ");

        Console.WriteLine(
            $"State : {_connection.State}");

        Console.WriteLine(
            "==========================================");

        // ========================================================
        // REGISTER DRIVER
        // ========================================================

        await RegisterDriverAsync();
    }

    // ============================================================
    // REGISTER DRIVER
    // ============================================================

    private async Task RegisterDriverAsync()
    {
        if (_connection == null)
            throw new Exception(
                "Connexion SignalR inexistante.");

        if (_connection.State !=
            HubConnectionState.Connected)
        {
            throw new Exception(
                $"SignalR non connecté. State={_connection.State}");
        }

        Console.WriteLine(
            "==========================================");

        Console.WriteLine(
            "REGISTER DRIVER");

        Console.WriteLine(
            $"DriverId : {_driverId}");

        Console.WriteLine(
            "==========================================");

        await _connection.InvokeAsync(
            "RegisterDriver",
            _driverId);

        Console.WriteLine(
            $"✅ Driver {_driverId} enregistré dans SignalR.");

        Console.WriteLine(
            $"Groupe attendu : driver-{_driverId}");
    }

    // ============================================================
    // STOP
    // ============================================================

    public async Task StopAsync()
    {
        try
        {
            if (_connection == null)
                return;

            Console.WriteLine(
                "SIGNALR : fermeture...");

            await _connection.StopAsync();

            await _connection.DisposeAsync();

            _connection = null;

            Console.WriteLine(
                "SIGNALR : connexion fermée.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR SIGNALR STOP : {ex}");
        }
    }

    // ============================================================
    // ETAT
    // ============================================================

    public bool IsConnected =>
        _connection?.State ==
        HubConnectionState.Connected;
}