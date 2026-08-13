using MoraTuk.Mobile.Models;
using MoraTuk.Mobile.Services;

namespace MoraTuk.Mobile.Pages;

public partial class DriverHomePage : ContentPage
{
    private readonly DriverHubService _hubService;
    private readonly int _driverId;

    private bool _started = false;

    public DriverHomePage(
        DriverHubService hubService,
        int driverId)
    {
        InitializeComponent();

        _hubService = hubService;
        _driverId = driverId;

        // ============================================================
        // NOUVELLE COURSE VIA SIGNALR
        // ============================================================

        _hubService.OnNewRideReceived = ride =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    AddRideCard(ride);

                    RideLabel.Text =
                        "Nouvelle course disponible";

                    await DisplayAlert(
                        "🚕 NOUVELLE COURSE",
                        $"Course #{ride.RideId}\n\n" +
                        $"📍 Départ : {ride.Departure}\n" +
                        $"🎯 Destination : {ride.Destination}\n\n" +
                        $"💰 Prix : {ride.Price:F0} Ar\n" +
                        $"📏 Distance : " +
                        $"{ride.DistanceToDriver:F2} km",
                        "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert(
                        "❌ ERREUR AFFICHAGE",
                        ex.ToString(),
                        "OK");
                }
            });
        };
    }


    // ============================================================
    // APPARITION PAGE
    // ============================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_started)
            return;

        _started = true;

        try
        {
            await DisplayAlert(
                "CHAUFFEUR",
                $"DriverHomePage ouverte.\n\n" +
                $"DriverId = {_driverId}",
                "OK");

            StatusLabel.Text =
                "Connexion au serveur...";

            await _hubService.StartAsync(
                _driverId);

            await LoadExistingRidesAsync();

            StatusLabel.Text =
                "Statut : En ligne 🟢";

            await DisplayAlert(
                "CHAUFFEUR ✅",
                $"DriverId = {_driverId}\n\n" +
                "Connexion réussie.\n\n" +
                "Les courses disponibles sont affichées.",
                "OK");
        }
        catch (Exception ex)
        {
            _started = false;

            StatusLabel.Text =
                "Statut : Hors ligne 🔴";

            await DisplayAlert(
                "❌ ERREUR CHAUFFEUR",
                ex.ToString(),
                "OK");
        }
    }


    // ============================================================
    // CHARGER COURSES EXISTANTES
    // ============================================================

    private async Task LoadExistingRidesAsync()
    {
        try
        {
            RideLabel.Text =
                "Chargement des courses...";

            RidesContainer.Children.Clear();

            var rideService =
                new RideService();

            var rides =
                await rideService
                    .GetAvailableRidesAsync(
                        _driverId);

            Console.WriteLine(
                $"Courses reçues : {rides.Count}");

            var driverRides =
                rides
                    .Where(x =>
                        x.DriverId == _driverId)
                    .OrderByDescending(x =>
                        x.RideId)
                    .ToList();

            Console.WriteLine(
                $"Courses pour DriverId {_driverId} : " +
                $"{driverRides.Count}");

            if (!driverRides.Any())
            {
                RideLabel.Text =
                    "Aucune course active";

                return;
            }

            foreach (var ride in driverRides)
            {
                AddRideCard(ride);
            }

            RideLabel.Text =
                $"{driverRides.Count} course(s) active(s)";
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR LOAD EXISTING RIDES : {ex}");

            RideLabel.Text =
                "Erreur lors du chargement";

            await DisplayAlert(
                "❌ ERREUR COURSES",
                ex.ToString(),
                "OK");
        }
    }


    // ============================================================
    // CRÉER CARTE COURSE
    // ============================================================

    private void AddRideCard(
        RideNotification ride)
    {
        var frame =
            new Frame
            {
                BackgroundColor =
                    Colors.White,

                CornerRadius = 20,

                Padding = 20,

                HasShadow = true
            };

        var layout =
            new VerticalStackLayout
            {
                Spacing = 10
            };

        var title =
            new Label
            {
                Text =
                    $"🚕 Course #{ride.RideId}",

                FontSize = 22,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Colors.Black
            };

        // ========================================================
        // STATUT
        // ========================================================

       var statusText =
            ride.Status switch
            {
                "WaitingDriver" =>
                    "🟡 Course disponible",

                "Accepted" =>
                    "🟢 Course acceptée",

                "InProgress" =>
                    "🚕 Course en cours",

                _ =>
                    $"ℹ️ {ride.Status}"
            };

        var status =
            new Label
            {
                Text = statusText,

                FontSize = 16,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    ride.Status == "Accepted" ||
                    ride.Status == "InProgress"
                        ? Colors.Green
                        : Colors.Orange
            };

        var separator =
            new BoxView
            {
                HeightRequest = 1,

                BackgroundColor =
                    Color.FromArgb("#DDDDDD")
            };

        var info =
            new Label
            {
                FontSize = 17,

                TextColor =
                    Color.FromArgb("#333333"),

                Text =
                    $"""
                    📍 Départ
                    {ride.Departure}

                    🎯 Destination
                    {ride.Destination}

                    📏 Distance chauffeur
                    {ride.DistanceToDriver:F2} km

                    👥 Passagers
                    {ride.Passengers}

                    🛺 Type
                    {ride.RideType}

                    💰 Prix
                    {ride.Price:F0} Ar
                    """
            };


        // ========================================================
        // BOUTON ACCEPTER
        // ========================================================

        var acceptButton =
            new Button
            {
                Text =
                    "✅ Accepter",

                BackgroundColor =
                    Colors.Green,

                TextColor =
                    Colors.White,

                HorizontalOptions =
                    LayoutOptions.Fill,

                WidthRequest = 130
            };


        // ========================================================
        // BOUTON REFUSER
        // ========================================================

        var rejectButton =
            new Button
            {
                Text =
                    "❌ Refuser",

                BackgroundColor =
                    Colors.Red,

                TextColor =
                    Colors.White,

                HorizontalOptions =
                    LayoutOptions.Fill,

                WidthRequest = 130
            };


        // ========================================================
        // BOUTON TERMINER
        // ========================================================

        var completeButton =
            new Button
            {
                Text =
                    "🏁 Course terminée",

                BackgroundColor =
                    Colors.DarkBlue,

                TextColor =
                    Colors.White,

                HorizontalOptions =
                    LayoutOptions.Fill,

                IsVisible =
                    false
            };

            // ========================================================
            // AFFICHAGE SELON LE STATUT
            // ========================================================

            if (ride.Status == "WaitingDriver")
            {
                acceptButton.IsVisible = true;
                rejectButton.IsVisible = true;
                completeButton.IsVisible = false;
            }
            else if (
                ride.Status == "Accepted" ||
                ride.Status == "InProgress")
            {
                acceptButton.IsVisible = false;
                rejectButton.IsVisible = false;
                completeButton.IsVisible = true;
            }
            else
            {
                acceptButton.IsVisible = false;
                rejectButton.IsVisible = false;
                completeButton.IsVisible = false;
            }
        
        // ========================================================
        // ACCEPTER
        // ========================================================

        acceptButton.Clicked +=
            async (sender, e) =>
            {
                try
                {
                    acceptButton.IsEnabled = false;
                    rejectButton.IsEnabled = false;

                    await DisplayAlert(
                        "ACCEPTER",
                        $"Tentative d'acceptation " +
                        $"de la course #{ride.RideId}.",
                        "OK");

                    var rideService =
                        new RideService();

                    var success =
                        await rideService.AcceptRideAsync(
                            ride.RideId,
                            _driverId);

                    if (!success)
                    {
                        acceptButton.IsEnabled = true;
                        rejectButton.IsEnabled = true;

                        await DisplayAlert(
                            "❌ ERREUR",
                            "La course n'est plus disponible.",
                            "OK");

                        return;
                    }

                    // =================================================
                    // COURSE ACCEPTÉE
                    // =================================================

                    status.Text =
                        "🟢 Course en cours";

                    status.TextColor =
                        Colors.DarkBlue;

                    acceptButton.IsVisible =
                        false;

                    rejectButton.IsVisible =
                        false;

                    completeButton.IsVisible =
                        true;

                    RideLabel.Text =
                        "Course en cours";

                    StatusLabel.Text =
                        "Statut : Occupé 🔴";

                    await DisplayAlert(
                        "✅ COURSE ACCEPTÉE",
                        $"Course #{ride.RideId} acceptée.\n\n" +
                        "Vous êtes maintenant en course.\n\n" +
                        "Quand vous arrivez à destination, " +
                        "appuyez sur « Course terminée ».",
                        "OK");
                }
                catch (Exception ex)
                {
                    acceptButton.IsEnabled = true;
                    rejectButton.IsEnabled = true;

                    await DisplayAlert(
                        "❌ ERREUR ACCEPTATION",
                        ex.ToString(),
                        "OK");
                }
            };


        // ========================================================
        // REFUSER
        // ========================================================

        rejectButton.Clicked +=
            async (sender, e) =>
            {
                try
                {
                    var confirm =
                        await DisplayAlert(
                            "Refuser la course",
                            $"Voulez-vous vraiment refuser " +
                            $"la course #{ride.RideId} ?",
                            "Oui",
                            "Non");

                    if (!confirm)
                        return;

                    var rideService =
                        new RideService();

                    var success =
                        await RejectRideAsync(
                            ride.RideId,
                            _driverId);

                    if (!success)
                    {
                        await DisplayAlert(
                            "❌ ERREUR",
                            "Impossible de refuser la course.",
                            "OK");

                        return;
                    }

                    RidesContainer.Children.Remove(
                        frame);

                    var remaining =
                        RidesContainer
                            .Children
                            .Count;

                    RideLabel.Text =
                        remaining > 0
                            ? $"{remaining} course(s) disponible(s)"
                            : "Aucune course disponible";

                    await DisplayAlert(
                        "COURSE REFUSÉE",
                        $"Course #{ride.RideId} refusée.",
                        "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert(
                        "❌ ERREUR REFUS",
                        ex.ToString(),
                        "OK");
                }
            };


        // ========================================================
        // TERMINER
        // ========================================================

        completeButton.Clicked +=
            async (sender, e) =>
            {
                try
                {
                    var confirm =
                        await DisplayAlert(
                            "🏁 Terminer la course",
                            $"Confirmez-vous que la course " +
                            $"#{ride.RideId} est terminée ?",
                            "Oui, terminer",
                            "Annuler");

                    if (!confirm)
                        return;

                    completeButton.IsEnabled =
                        false;

                    var rideService =
                        new RideService();

                    var success =
                        await rideService
                            .CompleteRideAsync(
                                ride.RideId,
                                _driverId);

                    if (!success)
                    {
                        completeButton.IsEnabled =
                            true;

                        await DisplayAlert(
                            "❌ ERREUR",
                            "Impossible de terminer la course.",
                            "OK");

                        return;
                    }

                    // =================================================
                    // COURSE TERMINÉE
                    // =================================================

                    status.Text =
                        "✅ Course terminée";

                    status.TextColor =
                        Colors.Green;

                    RideLabel.Text =
                        "Chauffeur disponible pour une nouvelle course";

                    StatusLabel.Text =
                        "Statut : En ligne 🟢";

                    await DisplayAlert(
                        "🏁 COURSE TERMINÉE",
                        $"Course #{ride.RideId} terminée.\n\n" +
                        "Vous êtes maintenant disponible " +
                        "pour une nouvelle course.",
                        "OK");

                    // -------------------------------------------------
                    // RETIRER LA CARTE
                    // -------------------------------------------------

                    RidesContainer.Children.Remove(
                        frame);

                    var remaining =
                        RidesContainer
                            .Children
                            .Count;

                    if (remaining == 0)
                    {
                        RideLabel.Text =
                            "Aucune course disponible";
                    }
                }
                catch (Exception ex)
                {
                    completeButton.IsEnabled =
                        true;

                    await DisplayAlert(
                        "❌ ERREUR TERMINAISON",
                        ex.ToString(),
                        "OK");
                }
            };


        // ========================================================
        // BOUTONS
        // ========================================================

        var buttons =
            new HorizontalStackLayout
            {
                Spacing = 10,

                HorizontalOptions =
                    LayoutOptions.Center
            };

        buttons.Children.Add(
            acceptButton);

        buttons.Children.Add(
            rejectButton);


        // ========================================================
        // BOUTON TERMINER
        // ========================================================

        var completeLayout =
            new VerticalStackLayout
            {
                Spacing = 5
            };

        completeLayout.Children.Add(
            completeButton);


        // ========================================================
        // CONSTRUCTION
        // ========================================================

        layout.Children.Add(
            title);

        layout.Children.Add(
            status);

        layout.Children.Add(
            separator);

        layout.Children.Add(
            info);

        layout.Children.Add(
            buttons);

        layout.Children.Add(
            completeLayout);

        frame.Content =
            layout;

        RidesContainer.Children.Add(
            frame);
    }


    // ============================================================
    // REFUSER UNE COURSE
    // ============================================================

    private async Task<bool> RejectRideAsync(
        int rideId,
        int driverId)
    {
        try
        {
            var rideService =
                new RideService();

            var url =
                $"{MoraTuk.Mobile.Helpers.ApiSettings.BaseUrl.TrimEnd('/')}" +
                $"/api/Ride/{rideId}/reject?driverId={driverId}";

            Console.WriteLine(
                $"REJECT RIDE URL : {url}");

            using var http =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(20)
                };

            var response =
                await http.PutAsync(
                    url,
                    null);

            var content =
                await response.Content
                    .ReadAsStringAsync();

            Console.WriteLine(
                $"REJECT STATUS : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"REJECT RESPONSE : {content}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR REJECT : {ex}");

            return false;
        }
    }


    // ============================================================
    // DISPARITION PAGE
    // ============================================================

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // On conserve SignalR.
        // Le chauffeur pourra continuer à recevoir
        // les nouvelles courses.
    }
}