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
                    // Ajouter la nouvelle course à la liste
                    AddRideCard(ride);

                    RideLabel.Text =
                        "Nouvelle course disponible";

                    await DisplayAlert(
                        "🚕 NOUVELLE COURSE",
                        $"Course #{ride.RideId}\n\n" +
                        $"📍 Départ : {ride.Departure}\n" +
                        $"🎯 Destination : {ride.Destination}\n\n" +
                        $"💰 Prix : {ride.Price:F0} Ar\n" +
                        $"📏 Distance : {ride.DistanceToDriver:F2} km",
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

            // --------------------------------------------------------
            // SIGNALR
            // --------------------------------------------------------

            await _hubService.StartAsync(
                _driverId);

            // --------------------------------------------------------
            // CHARGER LES COURSES EXISTANTES
            // --------------------------------------------------------

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
    // CHARGER LES COURSES EXISTANTES
    // ============================================================

    private async Task LoadExistingRidesAsync()
    {
        try
        {
            RideLabel.Text =
                "Chargement des courses...";

            // Nettoyer la liste actuelle
            RidesContainer.Children.Clear();

            var rideService =
                new RideService();

            // Appel :
            // GET /api/Ride/available/{driverId}

            var rides =
                await rideService
                    .GetAvailableRidesAsync(
                        _driverId);

            Console.WriteLine(
                $"Courses reçues : {rides.Count}");

            // Sécurité supplémentaire :
            // garder uniquement les courses du chauffeur
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

            // --------------------------------------------------------
            // AUCUNE COURSE
            // --------------------------------------------------------

            if (!driverRides.Any())
            {
                RideLabel.Text =
                    "Aucune course disponible";

                return;
            }

            // --------------------------------------------------------
            // AFFICHER TOUTES LES COURSES
            // --------------------------------------------------------

            foreach (var ride in driverRides)
            {
                AddRideCard(ride);
            }

            RideLabel.Text =
                $"{driverRides.Count} course(s) disponible(s)";
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
    // CREER UNE CARTE POUR UNE COURSE
    // ============================================================

    private void AddRideCard(
        RideNotification ride)
    {
        // --------------------------------------------------------
        // FRAME
        // --------------------------------------------------------

        var frame =
            new Frame
            {
                BackgroundColor =
                    Colors.White,

                CornerRadius = 20,

                Padding = 20,

                HasShadow = true
            };


        // --------------------------------------------------------
        // CONTENEUR
        // --------------------------------------------------------

        var layout =
            new VerticalStackLayout
            {
                Spacing = 10
            };


        // --------------------------------------------------------
        // TITRE
        // --------------------------------------------------------

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


        // --------------------------------------------------------
        // STATUT
        // --------------------------------------------------------

        var status =
            new Label
            {
                Text =
                    "🟡 Course disponible",

                FontSize = 16,

                TextColor =
                    Colors.Green
            };


        // --------------------------------------------------------
        // SEPARATEUR
        // --------------------------------------------------------

        var separator =
            new BoxView
            {
                HeightRequest = 1,

                BackgroundColor =
                    Color.FromArgb("#DDDDDD")
            };


        // --------------------------------------------------------
        // INFORMATIONS
        // --------------------------------------------------------

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


        // --------------------------------------------------------
        // BOUTON ACCEPTER
        // --------------------------------------------------------

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


        // --------------------------------------------------------
        // BOUTON REFUSER
        // --------------------------------------------------------

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
        // ACCEPTER
        // ========================================================

        acceptButton.Clicked +=
            async (sender, e) =>
            {
                try
                {
                    await DisplayAlert(
                        "ACCEPTER",
                        $"Tentative d'acceptation de la course #{ride.RideId}.",
                        "OK");

                    var rideService =
                        new RideService();

                    // IMPORTANT :
                    // Ici on pourra appeler ton API
                    // /api/Ride/{id}/accept?driverId={driverId}

                    var success =
                        await rideService.AcceptRideAsync(
                            ride.RideId,
                            _driverId);

                    if (success)
                    {
                        await DisplayAlert(
                            "✅ COURSE ACCEPTÉE",
                            $"Course #{ride.RideId} acceptée.",
                            "OK");

                        // Retirer la course de la liste
                        RidesContainer.Children.Remove(
                            frame);

                        RideLabel.Text =
                            "Course acceptée";
                    }
                    else
                    {
                        await DisplayAlert(
                            "❌ REFUS",
                            "La course n'est plus disponible.",
                            "OK");
                    }
                }
                catch (Exception ex)
                {
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


        // --------------------------------------------------------
        // BOUTONS
        // --------------------------------------------------------

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


        // --------------------------------------------------------
        // CONSTRUCTION CARTE
        // --------------------------------------------------------

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


        frame.Content =
            layout;


        // --------------------------------------------------------
        // AJOUT À LA LISTE
        // --------------------------------------------------------

        RidesContainer.Children.Add(
            frame);
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