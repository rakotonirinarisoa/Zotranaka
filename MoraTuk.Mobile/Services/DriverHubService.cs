using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using MoraTuk.Mobile.Helpers;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class DriverHubService
{
    private HubConnection? _connection;

    // Transmet la nouvelle course à DriverHomePage
    public Action<RideNotification>? OnNewRideReceived { get; set; }

    private async Task Alert(
        string title,
        string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    var page =
                        Application.Current?
                            .Windows?
                            .FirstOrDefault()?
                            .Page;

                    if (page != null)
                    {
                        await page.DisplayAlert(
                            title,
                            message,
                            "OK");
                    }
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Erreur DisplayAlert : {ex}");
        }
    }

    // ============================================================
    // START SIGNALR
    // ============================================================

    public async Task StartAsync(int driverId)
    {
        try
        {
            await Alert(
                "SIGNALR 1",
                $"DriverId : {driverId}");

            // ----------------------------------------------------
            // VÉRIFICATION DRIVER ID
            // ----------------------------------------------------

            if (driverId <= 0)
            {
                throw new Exception(
                    $"DriverId invalide : {driverId}");
            }

            // ----------------------------------------------------
            // VÉRIFICATION CONFIGURATION
            // ----------------------------------------------------

            await Alert(
                "SIGNALR 2",
                $"BaseUrl :\n{ApiSettings.BaseUrl}\n\n" +
                $"Configured : {ApiSettings.IsConfigured}");

            if (!ApiSettings.IsConfigured)
            {
                throw new Exception(
                    "ApiSettings.BaseUrl n'est pas configuré.");
            }

            // ----------------------------------------------------
            // SI DÉJÀ CONNECTÉ
            // ----------------------------------------------------

            if (_connection != null &&
                _connection.State ==
                HubConnectionState.Connected)
            {
                await Alert(
                    "SIGNALR",
                    "Déjà connecté.");

                return;
            }

            // ----------------------------------------------------
            // NETTOYAGE ANCIENNE CONNEXION
            // ----------------------------------------------------

            if (_connection != null)
            {
                try
                {
                    await _connection.DisposeAsync();
                }
                catch
                {
                    // Ignorer erreur de nettoyage
                }

                _connection = null;
            }

            // ----------------------------------------------------
            // URL SIGNALR
            // ----------------------------------------------------

            var hubUrl =
                $"{ApiSettings.BaseUrl.TrimEnd('/')}/trackingHub";

            await Alert(
                "SIGNALR 3",
                $"Hub URL :\n\n{hubUrl}");

            // ----------------------------------------------------
            // CRÉATION CONNEXION SIGNALR
            //
            // IMPORTANT :
            // On utilise uniquement LongPolling
            // pour éviter le problème WebSocket/Cloudflare.
            // ----------------------------------------------------

            _connection =
                new HubConnectionBuilder()
                    .WithUrl(
                        hubUrl,
                        options =>
                        {
                            options.Transports =
                                HttpTransportType.LongPolling;
                        })
                    .WithAutomaticReconnect(
                        new[]
                        {
                            TimeSpan.FromSeconds(0),
                            TimeSpan.FromSeconds(2),
                            TimeSpan.FromSeconds(5),
                            TimeSpan.FromSeconds(10)
                        })
                    .Build();

            // ====================================================
            // RÉCEPTION NOUVELLE COURSE
            // ====================================================

            _connection.On<RideNotification>(
                "NewRide",
                async ride =>
                {
                    try
                    {
                        Console.WriteLine(
                            $"NOUVELLE COURSE : {ride.RideId}");

                        await Alert(
                             "🚕 NOUVELLE COURSE",
                                $"Course #{ride.RideId}\n\n" +
                                $"📍 Départ : {ride.Departure}\n" +
                                $"🏁 Destination : {ride.Destination}\n\n" +
                                $"💰 Prix : {ride.Price:F0} Ar\n" +
                                $"📏 Distance chauffeur : {ride.DistanceToDriver:F2} km\n" +
                                $"👥 Passagers : {ride.Passengers}\n" +
                                $"🛺 Type : {ride.RideType}");

                        // Envoyer la course à DriverHomePage
                        if (OnNewRideReceived != null)
                        {
                            await MainThread.InvokeOnMainThreadAsync(
                                () =>
                                {
                                    OnNewRideReceived.Invoke(ride);
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"ERREUR NEW RIDE : {ex}");

                        await Alert(
                            "ERREUR NEW RIDE",
                            ex.ToString());
                    }
                });

            // ====================================================
            // RECONNEXION
            // ====================================================

            _connection.Reconnecting +=
                async error =>
                {
                    Console.WriteLine(
                        $"SignalR reconnexion : {error}");

                    await Alert(
                        "SIGNALR",
                        "Connexion perdue.\n\n" +
                        "Tentative de reconnexion...");
                };

            // ====================================================
            // RECONNECTÉ
            // ====================================================

            _connection.Reconnected +=
                async connectionId =>
                {
                    try
                    {
                        Console.WriteLine(
                            $"SignalR reconnecté : {connectionId}");

                        await Alert(
                            "SIGNALR RECONNECTÉ",
                            $"Connexion rétablie.\n\n" +
                            $"ConnectionId : {connectionId}");

                        if (_connection == null)
                            return;

                        // Réenregistrer le chauffeur
                        await _connection.InvokeAsync(
                            "RegisterDriver",
                            driverId);

                        await Alert(
                            "REGISTER DRIVER",
                            $"Driver {driverId} réenregistré.\n\n" +
                            $"Groupe : driver-{driverId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"ERREUR REGISTER APRÈS RECONNECT : {ex}");

                        await Alert(
                            "ERREUR REGISTER",
                            ex.ToString());
                    }
                };

            // ====================================================
            // FERMETURE
            // ====================================================

            _connection.Closed +=
                async error =>
                {
                    Console.WriteLine(
                        $"SignalR fermé : {error}");

                    await Alert(
                        "SIGNALR FERMÉ",
                        error?.ToString()
                        ?? "Connexion fermée.");
                };

            // ====================================================
            // START
            // ====================================================

            await Alert(
                "SIGNALR 4",
                "StartAsync() en cours...");

            try
            {
                await _connection.StartAsync();

                await Alert(
                    "SIGNALR 5 ✅",
                    $"Connexion réussie !\n\n" +
                    $"State : {_connection.State}");
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(
                    $"SIGNALR OPERATION CANCELED : {ex}");

                await Alert(
                    "SIGNALR ANNULÉ",
                    ex.ToString());

                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"SIGNALR START ERROR : {ex}");

                await Alert(
                    "SIGNALR START ERREUR",
                    ex.ToString());

                throw;
            }

            // ====================================================
            // REGISTER DRIVER
            // ====================================================

            await Alert(
                "SIGNALR 6",
                $"RegisterDriver({driverId})...");

            await _connection.InvokeAsync(
                "RegisterDriver",
                driverId);

            await Alert(
                "SIGNALR 7 ✅",
                $"Chauffeur enregistré !\n\n" +
                $"DriverId : {driverId}\n" +
                $"Groupe : driver-{driverId}\n\n" +
                "En attente d'une course.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"SIGNALR ERREUR : {ex}");

            await Alert(
                "❌ SIGNALR ERREUR",
                ex.ToString());

            throw;
        }
    }

    // ============================================================
    // STOP SIGNALR
    // ============================================================

    public async Task StopAsync()
    {
        try
        {
            if (_connection == null)
                return;

            await Alert(
                "SIGNALR STOP",
                "Fermeture de la connexion...");

            await _connection.StopAsync();

            await _connection.DisposeAsync();

            _connection = null;

            await Alert(
                "SIGNALR STOP",
                "Connexion fermée.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR SIGNALR STOP : {ex}");

            await Alert(
                "ERREUR SIGNALR STOP",
                ex.ToString());
        }
    }

    // ============================================================
    // ÉTAT CONNEXION
    // ============================================================

    public bool IsConnected =>
        _connection?.State ==
        HubConnectionState.Connected;
}

