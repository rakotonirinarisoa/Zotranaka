using MoraTuk.Mobile.Models;
using MoraTuk.Mobile.Services;

namespace MoraTuk.Mobile.Pages;

public partial class DriverHomePage : ContentPage
{
    private readonly DriverHubService _hubService;
    private readonly int _driverId;

    private bool _started = false;

    private readonly HashSet<int> _displayedRideIds = new();

    private readonly HashSet<int> _paymentMonitoring = new();

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

       _hubService.OnNewRideReceived =
            ride =>
            {
                MainThread.BeginInvokeOnMainThread(
                    async () =>
                    {
                        try
                        {
                            Console.WriteLine(
                                $"SIGNALR -> DriverHomePage : RideId={ride.RideId}");

                            Console.WriteLine(
                                $"SIGNALR -> Status={ride.Status}");

                            // ====================================================
                            // IMPORTANT
                            // ====================================================
                            // Une nouvelle course doit toujours être disponible
                            // pour le chauffeur.
                            //
                            // Si l'API envoie WaitingDriver, on garde cet état.
                            // ====================================================

                            if (string.IsNullOrWhiteSpace(ride.Status))
                            {
                                ride.Status = "WaitingDriver";
                            }

                            // ====================================================
                            // AJOUTER LA CARTE
                            // ====================================================

                            AddRideCard(ride);

                            RideLabel.Text =
                                "Nouvelle course disponible";

                            Console.WriteLine(
                                $"Carte course #{ride.RideId} ajoutée.");

                            await DisplayAlert(
                                "🚕 NOUVELLE COURSE",
                                $"Course #{ride.RideId}\n\n" +
                                $"📍 Départ : {ride.Departure}\n" +
                                $"🏁 Destination : {ride.Destination}\n\n" +
                                $"💰 Prix : {ride.Price:F0} Ar\n" +
                                $"📏 Distance : {ride.DistanceToDriver:F2} km",
                                "OK");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                $"ERREUR AFFICHAGE SIGNALR : {ex}");

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
            StatusLabel.Text =
                "Connexion au serveur...";

            await _hubService.StartAsync(
                _driverId);

            await LoadExistingRidesAsync();

            StatusLabel.Text =
                "Statut : En ligne 🟢";
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

            _displayedRideIds.Clear();

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
                    .OrderByDescending(
                        x => x.RideId)
                    .ToList();

            Console.WriteLine(
                $"Courses pour DriverId {_driverId} : " +
                $"{driverRides.Count}");

            foreach (var ride in driverRides)
            {
                AddRideCard(ride);
            }

            if (!driverRides.Any())
            {
                RideLabel.Text =
                    "Aucune course disponible";

                return;
            }

            RideLabel.Text =
                $"{driverRides.Count} course(s)";
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR LOAD RIDES : {ex}");

            RideLabel.Text =
                "Erreur lors du chargement";

            await DisplayAlert(
                "❌ ERREUR COURSES",
                ex.ToString(),
                "OK");
        }
    }


    // ============================================================
    // AJOUTER CARTE COURSE
    // ============================================================

    private void AddRideCard(
        RideNotification ride)
    {
        if (ride == null)
            return;

        // ========================================================
        // EVITER DOUBLON
        // ========================================================

        if (_displayedRideIds.Contains(
            ride.RideId))
        {
            Console.WriteLine(
                $"Course #{ride.RideId} déjà affichée.");

            return;
        }

        _displayedRideIds.Add(
            ride.RideId);


        // ========================================================
        // FRAME
        // ========================================================

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


        // ========================================================
        // TITRE
        // ========================================================

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

        var status =
            new Label
            {
                FontSize = 16,

                FontAttributes =
                    FontAttributes.Bold
            };

        UpdateStatusLabel(
            ride,
            status);


        // ========================================================
        // SEPARATOR
        // ========================================================

        var separator =
            new BoxView
            {
                HeightRequest = 1,

                BackgroundColor =
                    Color.FromArgb(
                        "#DDDDDD")
            };


        // ========================================================
        // INFOS
        // ========================================================

        var info =
            new Label
            {
                FontSize = 17,

                TextColor =
                    Color.FromArgb(
                        "#333333"),

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

                IsVisible = false
            };


        // ========================================================
        // AFFICHAGE SELON STATUT
        // ========================================================

        ApplyRideState(
            ride,
            acceptButton,
            rejectButton,
            completeButton);


        // ========================================================
        // ACCEPTER
        // ========================================================

        acceptButton.Clicked +=
            async (_, _) =>
            {
                try
                {
                    acceptButton.IsEnabled =
                        false;

                    rejectButton.IsEnabled =
                        false;

                    var rideService =
                        new RideService();

                    var result =
                        await rideService
                            .AcceptRideAsync(
                                ride.RideId,
                                _driverId);

                    if (!result.Success)
                    {
                        acceptButton.IsEnabled =
                            true;

                        rejectButton.IsEnabled =
                            true;

                        await DisplayAlert(
                            "❌ ERREUR ACCEPTATION",
                            $"HTTP : {result.StatusCode}\n\n" +
                            $"Message : {result.Message}\n\n" +
                            $"Réponse API :\n" +
                            result.ResponseBody,
                            "OK");

                        return;
                    }


                    // =================================================
                    // COURSE ACCEPTÉE
                    // =================================================

                    ride.Status =
                        "Accepted";

                    status.Text =
                        "🟡 Paiement MVola en attente";

                    status.TextColor =
                        Colors.Orange;

                    acceptButton.IsVisible =
                        false;

                    rejectButton.IsVisible =
                        false;

                    completeButton.IsVisible =
                        false;

                    RideLabel.Text =
                        "Paiement MVola en attente...";

                    StatusLabel.Text =
                        "Statut : Paiement MVola 🟡";


                    await DisplayAlert(
                        "✅ COURSE ACCEPTÉE",
                        $"Course #{ride.RideId} acceptée.\n\n" +
                        "Le paiement MVola est en attente de confirmation.",
                        "OK");


                    // =================================================
                    // MONITORING PAIEMENT
                    // =================================================

                    if (_paymentMonitoring.Add(
                        ride.RideId))
                    {
                        _ = MonitorPaymentAsync(
                            ride,
                            status,
                            acceptButton,
                            rejectButton,
                            completeButton,
                            frame);
                    }
                }
                catch (Exception ex)
                {
                    acceptButton.IsEnabled =
                        true;

                    rejectButton.IsEnabled =
                        true;

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
            async (_, _) =>
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


                    acceptButton.IsEnabled =
                        false;

                    rejectButton.IsEnabled =
                        false;


                    var rideService =
                        new RideService();

                    var result =
                        await rideService
                            .RejectRideAsync(
                                ride.RideId,
                                _driverId);


                    if (!result.Success)
                    {
                        acceptButton.IsEnabled =
                            true;

                        rejectButton.IsEnabled =
                            true;

                        await DisplayAlert(
                            "❌ ERREUR REFUS",
                            $"HTTP : {result.StatusCode}\n\n" +
                            $"Message : {result.Message}\n\n" +
                            $"Réponse API :\n" +
                            result.ResponseBody,
                            "OK");

                        return;
                    }


                    RemoveRideCard(
                        ride.RideId,
                        frame);


                    RideLabel.Text =
                        "Aucune course disponible";


                    await DisplayAlert(
                        "COURSE REFUSÉE",
                        $"Course #{ride.RideId} refusée.",
                        "OK");
                }
                catch (Exception ex)
                {
                    acceptButton.IsEnabled =
                        true;

                    rejectButton.IsEnabled =
                        true;

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
            async (_, _) =>
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


                    ride.Status =
                        "Completed";


                    status.Text =
                        "✅ Course terminée";

                    status.TextColor =
                        Colors.Green;

                    StatusLabel.Text =
                        "Statut : En ligne 🟢";

                    RideLabel.Text =
                        "Chauffeur disponible";


                    RemoveRideCard(
                        ride.RideId,
                        frame);


                    await DisplayAlert(
                        "🏁 COURSE TERMINÉE",
                        $"Course #{ride.RideId} terminée.\n\n" +
                        "Vous êtes maintenant disponible.",
                        "OK");
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
            completeButton);


        frame.Content =
            layout;


        RidesContainer.Children.Add(
            frame);
    }


    // ============================================================
    // MONITOR PAYMENT
    // ============================================================

    private async Task MonitorPaymentAsync(
        RideNotification ride,
        Label status,
        Button acceptButton,
        Button rejectButton,
        Button completeButton,
        Frame frame)
    {
        try
        {
            const int maxAttempts = 30;

            var rideService =
                new RideService();

            RideLabel.Text =
                "Paiement MVola en attente...";

            StatusLabel.Text =
                "Statut : Paiement MVola 🟡";

            status.Text =
                "🟡 Paiement MVola en attente";

            status.TextColor =
                Colors.Orange;

            completeButton.IsVisible =
                false;


            for (int attempt = 1;
                 attempt <= maxAttempts;
                 attempt++)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(5));


                var result =
                    await rideService
                        .GetPaymentStatusAsync(
                            ride.RideId);


                if (result == null)
                {
                    continue;
                }


                Console.WriteLine(
                    $"PAYMENT CHECK #{attempt}");

                Console.WriteLine(
                    $"RideStatus = {result.RideStatus}");

                Console.WriteLine(
                    $"PaymentStatus = {result.PaymentStatus}");

                Console.WriteLine(
                    $"MvolaStatus = {result.MvolaStatus}");


                // ====================================================
                // PAIEMENT CONFIRMÉ
                // ====================================================

                if (result.Confirmed &&
                    string.Equals(
                        result.PaymentStatus,
                        "Success",
                        StringComparison.OrdinalIgnoreCase))
                {
                    ride.Status =
                        "InProgress";

                    status.Text =
                        "🚕 Course en cours";

                    status.TextColor =
                        Colors.DarkBlue;

                    completeButton.IsVisible =
                        true;

                    completeButton.IsEnabled =
                        true;

                    RideLabel.Text =
                        "Course en cours";

                    StatusLabel.Text =
                        "Statut : Occupé 🔴";


                    await DisplayAlert(
                        "💰 PAIEMENT CONFIRMÉ",
                        $"Le paiement MVola de la course " +
                        $"#{ride.RideId} est confirmé.\n\n" +
                        "La course peut maintenant commencer.",
                        "OK");


                    _paymentMonitoring.Remove(
                        ride.RideId);

                    return;
                }


                // ====================================================
                // PAIEMENT ÉCHOUÉ
                // ====================================================

                if (result.Failed ||
                    string.Equals(
                        result.PaymentStatus,
                        "Failed",
                        StringComparison.OrdinalIgnoreCase))
                {
                    // =================================================
                    // RÉINITIALISER LA COURSE
                    // =================================================

                    ride.Status = "WaitingDriver";

                    // =================================================
                    // STATUT AFFICHÉ
                    // =================================================

                    status.Text =
                        "🟡 Course disponible - paiement échoué";

                    status.TextColor =
                        Colors.Orange;

                    // =================================================
                    // RÉAFFICHER LES BOUTONS
                    // =================================================

                    acceptButton.IsVisible = true;
                    acceptButton.IsEnabled = true;

                    rejectButton.IsVisible = true;
                    rejectButton.IsEnabled = true;

                    // =================================================
                    // TERMINER IMPOSSIBLE
                    // =================================================

                    completeButton.IsVisible = false;
                    completeButton.IsEnabled = false;

                    // =================================================
                    // CHAUFFEUR DISPONIBLE
                    // =================================================

                    RideLabel.Text =
                        "Course disponible - vous pouvez réessayer";

                    StatusLabel.Text =
                        "Statut : En ligne 🟢";

                    // =================================================
                    // ARRÊTER UNIQUEMENT LE MONITORING
                    // =================================================

                    _paymentMonitoring.Remove(
                        ride.RideId);

                    await DisplayAlert(
                        "❌ PAIEMENT ÉCHOUÉ",
                        $"Le paiement MVola de la course " +
                        $"#{ride.RideId} a échoué.\n\n" +
                        "La course est de nouveau disponible.\n" +
                        "Vous pouvez tenter de l'accepter à nouveau.",
                        "OK");

                    return;
                }


                // ====================================================
                // PENDING
                // ====================================================

                status.Text =
                    "🟡 Paiement MVola en attente...";

                RideLabel.Text =
                    $"Vérification du paiement...\n" +
                    $"Tentative {attempt}/{maxAttempts}";
            }


            // ========================================================
            // TIMEOUT
            // ========================================================

            status.Text =
                "⚠️ Paiement toujours en attente";

            status.TextColor =
                Colors.Orange;

            completeButton.IsVisible =
                false;

            RideLabel.Text =
                "Paiement MVola toujours en attente";

            StatusLabel.Text =
                "Statut : Paiement en attente 🟡";


            await DisplayAlert(
                "⏳ PAIEMENT EN ATTENTE",
                $"Le paiement de la course #{ride.RideId} " +
                "n'est toujours pas confirmé.",
                "OK");


            _paymentMonitoring.Remove(
                ride.RideId);
        }
        catch (Exception ex)
        {
            _paymentMonitoring.Remove(
                ride.RideId);

            Console.WriteLine(
                $"ERREUR MONITOR PAYMENT : {ex}");

            await DisplayAlert(
                "❌ ERREUR PAIEMENT",
                ex.ToString(),
                "OK");
        }
    }


    // ============================================================
    // STATUT
    // ============================================================

    private static void UpdateStatusLabel(
        RideNotification ride,
        Label status)
    {
        switch (ride.Status)
        {
            case "WaitingDriver":

                status.Text =
                    "🟡 Course disponible";

                status.TextColor =
                    Colors.Orange;

                break;


            case "Accepted":

                status.Text =
                    "🟡 Paiement MVola en attente";

                status.TextColor =
                    Colors.Orange;

                break;


            case "InProgress":

                status.Text =
                    "🚕 Course en cours";

                status.TextColor =
                    Colors.DarkBlue;

                break;


            case "PaymentFailed":

                status.Text =
                    "❌ Paiement échoué";

                status.TextColor =
                    Colors.Red;

                break;


            default:

                status.Text =
                    $"ℹ️ {ride.Status}";

                status.TextColor =
                    Colors.Gray;

                break;
        }
    }


    // ============================================================
    // ETAT DES BOUTONS
    // ============================================================

    private static void ApplyRideState(
        RideNotification ride,
        Button accept,
        Button reject,
        Button complete)
        {
            // Tout cacher d'abord
            accept.IsVisible = false;
            reject.IsVisible = false;
            complete.IsVisible = false;

            accept.IsEnabled = true;
            reject.IsEnabled = true;
            complete.IsEnabled = true;

            // ============================================================
            // NOUVELLE COURSE
            // ============================================================

            if (string.Equals(
                    ride.Status,
                    "WaitingDriver",
                    StringComparison.OrdinalIgnoreCase))
            {
                accept.IsVisible = true;
                accept.IsEnabled = true;
                reject.IsVisible = true;
                 reject.IsEnabled = true;

                Console.WriteLine(
                    $"UI : Course #{ride.RideId} => ACCEPT / REFUSER visibles");

                return;
            }

            // ============================================================
            // COURSE ACCEPTÉE
            // ============================================================

            if (string.Equals(
                    ride.Status,
                    "Accepted",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Paiement MVola en attente
                accept.IsVisible = false;
                reject.IsVisible = false;
                complete.IsVisible = false;

                return;
            }

            // ============================================================
            // COURSE EN COURS
            // ============================================================

            if (string.Equals(
                    ride.Status,
                    "InProgress",
                    StringComparison.OrdinalIgnoreCase))
            {
                accept.IsVisible = false;
                reject.IsVisible = false;
                complete.IsVisible = true;

                return;
            }
        }


    // ============================================================
    // SUPPRIMER CARTE
    // ============================================================

    private void RemoveRideCard(
        int rideId,
        Frame frame)
    {
        _displayedRideIds.Remove(
            rideId);

        _paymentMonitoring.Remove(
            rideId);

        MainThread.BeginInvokeOnMainThread(
            () =>
            {
                if (RidesContainer.Children
                    .Contains(frame))
                {
                    RidesContainer.Children.Remove(
                        frame);
                }
            });
    }


    // ============================================================
    // DISPARITION
    // ============================================================

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // SignalR reste connecté.
    }
}