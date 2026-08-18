using Microsoft.Maui.Devices;
using MoraTuk.Mobile.Services;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Pages;

public partial class ClientHomePage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly RideService _rideService;
    private readonly LocationService _locationService;
    private readonly DistanceService _distanceService;
    private readonly LocationSearchService _searchService;

    // ============================================================
    // DESTINATION
    // ============================================================
   
    private double destinationLatitude = 0;
    private double destinationLongitude = 0;

    // Position actuelle du client, mise a jour a chaque fix GPS reussi,
    // reutilisee pour tracer la route des qu'une destination est choisie
    private double _clientLatitude = 0;
    private double _clientLongitude = 0;

    private CancellationTokenSource? _searchCancellation;
    private bool _mvolaPaymentPendingShown = false;
    private TaskCompletionSource<bool> _mapReadyTcs = new();

    // ============================================================
    // SESSION
    // ============================================================

    private int _clientId;
    private CancellationTokenSource? _rideMonitoringCancellation;
    private int _currentRideId = 0;

    // ============================================================
    // CONSTRUCTEUR
    // ============================================================

    public ClientHomePage(
        RideService rideService,
        LocationService locationService,
        DistanceService distanceService,
        LocationSearchService searchService,
        ApiService apiService)
    {
        InitializeComponent();

        _rideService = rideService;
        _locationService = locationService;
        _distanceService = distanceService;
        _searchService = searchService;
        _apiService = apiService;

        _ = InitializeMap();

        Console.WriteLine(
            "CLIENT HOME PAGE INITIALISEE");
    }

    // ============================================================
    // SESSION CLIENT
    // ============================================================

    private async Task LoadSessionAsync()
    {
        try
        {
            var id =
                await SecureStorage.GetAsync("userId");

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new Exception(
                    "Aucun userId trouvé dans la session.");
            }

            if (!int.TryParse(id, out var clientId))
            {
                throw new Exception(
                    $"userId invalide : {id}");
            }

            _clientId = clientId;

            Console.WriteLine(
                $"CLIENT SESSION CHARGEE : {_clientId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR SESSION CLIENT : {ex}");

            _clientId = 0;

            await DisplayAlert(
                "Erreur session",
                ex.Message,
                "OK");
        }
    }

    // ============================================================
    // CALCUL DU PRIX
    // ============================================================

    private async void CalculatePrice_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (_clientId <= 0)
            {
                await DisplayAlert(
                    "Erreur",
                    "Utilisateur non connecté.",
                    "OK");

                return;
            }

            if (destinationLatitude == 0 ||
                destinationLongitude == 0)
            {
                await DisplayAlert(
                    "Destination",
                    "Veuillez sélectionner une destination.",
                    "OK");

                return;
            }

            var location =
                await _locationService.GetLastLocationAsync(
                    _clientId);

            if (location == null)
            {
                await DisplayAlert(
                    "GPS",
                    "Position client introuvable.",
                    "OK");

                return;
            }

            var rideType =
                RideTypePicker.SelectedIndex == 1
                    ? "Private"
                    : "Shared";

            var dto = new CreateRideDto
            {
                ClientId = _clientId,

                PickupLatitude =
                    location.Latitude,

                PickupLongitude =
                    location.Longitude,

                DestinationLatitude =
                    destinationLatitude,

                DestinationLongitude =
                    destinationLongitude,

                Departure =
                    string.IsNullOrWhiteSpace(
                        DepartureLabel.Text)
                        ? "Position actuelle"
                        : DepartureLabel.Text,

                Destination =
                    string.IsNullOrWhiteSpace(
                        DestinationEntry.Text)
                        ? "Destination"
                        : DestinationEntry.Text,

                RideType = rideType
            };

            Console.WriteLine(
                "========== CALCUL PRIX ==========");

            Console.WriteLine(
                $"ClientId : {dto.ClientId}");

            Console.WriteLine(
                $"Pickup : " +
                $"{dto.PickupLatitude}, " +
                $"{dto.PickupLongitude}");

            Console.WriteLine(
                $"Destination : " +
                $"{dto.DestinationLatitude}, " +
                $"{dto.DestinationLongitude}");

            Console.WriteLine(
                $"RideType : {dto.RideType}");

            Console.WriteLine(
                "=================================");

            var result =
                await _apiService.EstimatePriceAsync(dto);

            if (result == null)
            {
                await DisplayAlert(
                    "Erreur",
                    "L'API n'a pas retourné de résultat pour le calcul du prix.",
                    "OK");

                return;
            }

            PriceLabel.Text =
                $"📍 Distance : {result.DistanceKm:F2} km\n" +
                $"💰 Prix estimé : {result.Price:F0} Ar";

            // ----------------------------------------------------
            // TRACER LA ROUTE SUR LA CARTE
            // ----------------------------------------------------

            DrawRouteOnMap(
                location.Latitude,
                location.Longitude,
                destinationLatitude,
                destinationLongitude);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR CALCUL PRIX : {ex}");

            await DisplayAlert(
                "Erreur calcul",
                ex.ToString(),
                "OK");
        }
    }

    // ============================================================
    // RECHERCHE CHAUFFEUR + CREATION COURSE
    // ============================================================

    private async void SearchDriver_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            // ----------------------------------------------------
            // CLIENT
            // ----------------------------------------------------

            if (_clientId <= 0)
            {
                await DisplayAlert(
                    "Erreur",
                    "Utilisateur non connecté.",
                    "OK");

                return;
            }

            // ----------------------------------------------------
            // DESTINATION
            // ----------------------------------------------------

            if (destinationLatitude == 0 ||
                destinationLongitude == 0)
            {
                await DisplayAlert(
                    "Destination",
                    "Veuillez choisir une destination.",
                    "OK");

                return;
            }

            // ----------------------------------------------------
            // NUMERO MVOLA CLIENT
            // ----------------------------------------------------

            var mvolaNumber =
                MvolaNumberEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(mvolaNumber))
            {
                await DisplayAlert(
                    "MVola",
                    "Veuillez saisir votre numéro MVola.",
                    "OK");

                return;
            }

            // Nettoyage numéro
            mvolaNumber =
                mvolaNumber
                    .Replace(" ", "")
                    .Replace("-", "");

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                "CREATION COURSE CLIENT");

            Console.WriteLine(
                $"CLIENT ID : {_clientId}");

            Console.WriteLine(
                $"MVOLA CLIENT : {mvolaNumber}");

            Console.WriteLine(
                "====================================");

            // ----------------------------------------------------
            // GPS ACTUEL
            // ----------------------------------------------------

            StatusLabel.Text =
                "📍 Récupération de votre position...";

            var location =
                await _locationService.GetCurrentLocation();

            if (location == null)
            {
                await DisplayAlert(
                    "GPS",
                    "Impossible de récupérer votre position GPS.",
                    "OK");

                return;
            }

            Console.WriteLine(
                $"GPS CLIENT : " +
                $"{location.Latitude}, " +
                $"{location.Longitude}");

            DepartureLabel.Text =
                $"📍 GPS : {location.Latitude:F6}, {location.Longitude:F6}";

            // ----------------------------------------------------
            // SAUVEGARDE POSITION CLIENT
            // ----------------------------------------------------

            await _locationService.SaveUserLocationAsync(
                _clientId,
                location.Latitude,
                location.Longitude);

            // ----------------------------------------------------
            // TYPE DE COURSE
            // ----------------------------------------------------

            var rideType =
                RideTypePicker.SelectedIndex == 1
                    ? "Private"
                    : "Shared";

            // ----------------------------------------------------
            // DTO
            // ----------------------------------------------------

            var dto = new CreateRideDto
            {
                ClientId =
                    _clientId,

                Departure =
                    string.IsNullOrWhiteSpace(
                        DepartureLabel.Text)
                        ? "Position actuelle"
                        : DepartureLabel.Text,

                PickupLatitude =
                    location.Latitude,

                PickupLongitude =
                    location.Longitude,

                Destination =
                    string.IsNullOrWhiteSpace(
                        DestinationEntry.Text)
                        ? "Destination"
                        : DestinationEntry.Text,

                DestinationLatitude =
                    destinationLatitude,

                DestinationLongitude =
                    destinationLongitude,

                RideType =
                    rideType,

                Status =
                    "WaitingDriver",

                // IMPORTANT :
                // numéro MVola personnel du CLIENT
                DebitMsisdn =
                    mvolaNumber
            };

            // ----------------------------------------------------
            // DEBUG
            // ----------------------------------------------------

            Console.WriteLine(
                "========== CREATE RIDE ==========");

            Console.WriteLine(
                $"ClientId    : {dto.ClientId}");

            Console.WriteLine(
                $"DebitMsisdn : {dto.DebitMsisdn}");

            Console.WriteLine(
                $"Pickup      : " +
                $"{dto.PickupLatitude}, " +
                $"{dto.PickupLongitude}");

            Console.WriteLine(
                $"Destination : " +
                $"{dto.DestinationLatitude}, " +
                $"{dto.DestinationLongitude}");

            Console.WriteLine(
                $"Departure   : {dto.Departure}");

            Console.WriteLine(
                $"Destination : {dto.Destination}");

            Console.WriteLine(
                $"RideType    : {dto.RideType}");

            Console.WriteLine(
                "=================================");

            // ----------------------------------------------------
            // CREATION COURSE
            // ----------------------------------------------------

            StatusLabel.Text =
                "🚕 Recherche d'un chauffeur...";

            var result =
                await _rideService.CreateRideAsync(dto);

            if (result == null)
            {
                StatusLabel.Text =
                    "❌ Impossible de créer la course.";

                await DisplayAlert(
                    "Erreur",
                    "Impossible de créer la course.",
                    "OK");

                return;
            }

            // ----------------------------------------------------
            // COURSE ACTIVE
            // ----------------------------------------------------
            _currentRideId = result.Id;

            Console.WriteLine(
                $"CLIENT : course active = #{_currentRideId}");

            _StartRideMonitoring(_currentRideId);

            // ----------------------------------------------------
            // RESULTAT
            // ----------------------------------------------------

            StatusLabel.Text =
                string.IsNullOrWhiteSpace(result.Driver)
                    ? "🚕 Recherche d'un chauffeur..."
                    : $"🚕 Chauffeur trouvé : {result.Driver}";

            await DisplayAlert(
                "Course créée",
                $"Prix : {result.Price} Ar\n" +
                $"Statut : {result.Status}",
                "OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR RECHERCHE CHAUFFEUR : {ex}");

            await DisplayAlert(
                "Erreur recherche chauffeur",
                ex.ToString(),
                "OK");
        }
    }

    // ============================================================
    // RECHERCHE DESTINATION AUTOMATIQUE
    // ============================================================

    private async void DestinationEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        try
        {
            var text =
                e.NewTextValue?.Trim();

            Console.WriteLine(
                $"TEXT DESTINATION : '{text}'");

            // ----------------------------------------------------
            // CHAMP VIDE
            // ----------------------------------------------------

            if (string.IsNullOrWhiteSpace(text))
            {
                DestinationList.ItemsSource = null;
                DestinationList.IsVisible = false;

                destinationLatitude = 0;
                destinationLongitude = 0;

                ClearDestinationOnMap();

                return;
            }

            // ----------------------------------------------------
            // ANNULER ANCIENNE RECHERCHE
            // ----------------------------------------------------

            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();

            _searchCancellation =
                new CancellationTokenSource();

            var token =
                _searchCancellation.Token;

            // ----------------------------------------------------
            // ATTENTE 300 MS
            // ----------------------------------------------------

            await Task.Delay(
                300,
                token);

            if (token.IsCancellationRequested)
                return;

            // ----------------------------------------------------
            // APPEL API
            // ----------------------------------------------------

            Console.WriteLine(
                $"RECHERCHE LOCATION : {text}");

            // IMPORTANT :
            // NE PAS ajouter "Toamasina".
            var places =
                await _searchService.SearchAsync(
                    text);

            if (token.IsCancellationRequested)
                return;

            // ----------------------------------------------------
            // RESULTATS
            // ----------------------------------------------------

            Console.WriteLine(
                $"RESULTATS LOCATION : " +
                $"{places?.Count ?? 0}");

            if (places == null ||
                places.Count == 0)
            {
                DestinationList.ItemsSource = null;
                DestinationList.IsVisible = false;

                Console.WriteLine(
                    "AUCUN RESULTAT DESTINATION.");

                return;
            }

            // ----------------------------------------------------
            // AFFICHER LA LISTE
            // ----------------------------------------------------

            DestinationList.ItemsSource =
                places;

            DestinationList.IsVisible =
                true;

            Console.WriteLine(
                $"LISTE DESTINATION AFFICHÉE : " +
                $"{places.Count}");

            foreach (var place in places)
            {
                Console.WriteLine(
                    $"  -> {place.Name} " +
                    $"({place.Latitude}, " +
                    $"{place.Longitude})");
            }
        }
        catch (TaskCanceledException)
        {
            // Normal :
            // une nouvelle frappe annule la recherche précédente.
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR RECHERCHE DESTINATION : {ex}");

            DestinationList.ItemsSource = null;
            DestinationList.IsVisible = false;

            await DisplayAlert(
                "Erreur recherche",
                ex.Message,
                "OK");
        }
    }

    // ============================================================
    // SELECTION DESTINATION
    // ============================================================

    private async void Destination_Selected(
        object sender,
        SelectionChangedEventArgs e)
    {
        try
        {
            if (e.CurrentSelection == null ||
                e.CurrentSelection.Count == 0)
            {
                return;
            }

            var place =
                e.CurrentSelection[0]
                as LocationDto;

            if (place == null)
            {
                Console.WriteLine(
                    "DESTINATION : objet LocationDto invalide.");

                return;
            }

            Console.WriteLine(
                "========== DESTINATION ==========");

            Console.WriteLine(
                $"ID : {place.Id}");

            Console.WriteLine(
                $"NAME : {place.Name}");

            Console.WriteLine(
                $"LAT : {place.Latitude}");

            Console.WriteLine(
                $"LON : {place.Longitude}");

            Console.WriteLine(
                "=================================");

            // ----------------------------------------------------
            // SAUVEGARDE DESTINATION
            // ----------------------------------------------------

            destinationLatitude =
                place.Latitude;

            destinationLongitude =
                place.Longitude;

            // ----------------------------------------------------
            // AFFICHAGE
            // ----------------------------------------------------

            DestinationEntry.Text =
                place.Name;

            StatusLabel.Text =
                $"📍 Destination : {place.Name}";

            // ----------------------------------------------------
            // CACHER LISTE
            // ----------------------------------------------------

            DestinationList.IsVisible =
                false;

            DestinationList.SelectedItem =
                null;

            // ----------------------------------------------------
            // MARQUEUR DESTINATION SUR LA CARTE
            // ----------------------------------------------------

            SetDestinationMarker(
                destinationLatitude,
                destinationLongitude);

            if (_clientLatitude != 0 &&
                _clientLongitude != 0)
            {
                DrawRouteOnMap(
                    _clientLatitude,
                    _clientLongitude,
                    destinationLatitude,
                    destinationLongitude);
            }

            // ----------------------------------------------------
            // VERIFICATION
            // ----------------------------------------------------

            Console.WriteLine(
                $"DESTINATION SELECTIONNEE : " +
                $"{place.Name}");

            Console.WriteLine(
                $"COORDONNEES : " +
                $"{destinationLatitude}, " +
                $"{destinationLongitude}");

            // Pas besoin d'une popup ici.
            // L'utilisateur voit directement la destination
            // sélectionnée dans le champ.
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR SELECTION DESTINATION : {ex}");

            await DisplayAlert(
                "Erreur sélection",
                ex.ToString(),
                "OK");
        }
    }

    // ============================================================
    // RECHERCHE DESTINATION MANUELLE
    // ============================================================

    private async void SearchDestination_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            var text =
                DestinationEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                await DisplayAlert(
                    "Destination",
                    "Entrez une destination.",
                    "OK");

                return;
            }

            Console.WriteLine(
                $"RECHERCHE MANUELLE : {text}");

            // IMPORTANT :
            // Ne pas ajouter "Toamasina".
            var places =
                await _searchService.SearchAsync(
                    text);

            if (places == null ||
                places.Count == 0)
            {
                await DisplayAlert(
                    "Destination",
                    "Aucun résultat trouvé.",
                    "OK");

                return;
            }

            var names =
                places
                    .Select(p => p.Name)
                    .ToArray();

            var selected =
                await DisplayActionSheet(
                    "Choisir une destination",
                    "Annuler",
                    null,
                    names);

            if (string.IsNullOrEmpty(selected) ||
                selected == "Annuler")
            {
                return;
            }

            var place =
                places.FirstOrDefault(
                    p => p.Name == selected);

            if (place == null)
                return;

            destinationLatitude =
                place.Latitude;

            destinationLongitude =
                place.Longitude;

            DestinationEntry.Text =
                place.Name;

            StatusLabel.Text =
                $"📍 Destination : {place.Name}";

            DestinationList.IsVisible =
                false;

            SetDestinationMarker(
                destinationLatitude,
                destinationLongitude);

            if (_clientLatitude != 0 &&
                _clientLongitude != 0)
            {
                DrawRouteOnMap(
                    _clientLatitude,
                    _clientLongitude,
                    destinationLatitude,
                    destinationLongitude);
            }

            Console.WriteLine(
                $"DESTINATION MANUELLE : " +
                $"{place.Name}");

            Console.WriteLine(
                $"LAT : {destinationLatitude}");

            Console.WriteLine(
                $"LON : {destinationLongitude}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR DESTINATION : {ex}");

            await DisplayAlert(
                "Erreur destination",
                ex.ToString(),
                "OK");
        }
    }

    // ============================================================
    // PAGE APPARAIT
    // ============================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // ----------------------------------------------------
            // SESSION
            // ----------------------------------------------------

            await LoadSessionAsync();

            if (_clientId <= 0)
            {
                DepartureLabel.Text =
                    "❌ Client non connecté.";

                return;
            }

            Console.WriteLine(
                $"CLIENT ID : {_clientId}");

            // ----------------------------------------------------
            // GPS - POSITION APPROXIMATIVE INSTANTANEE (CACHE)
            // ----------------------------------------------------
            // Affiche immediatement le point bleu avec la derniere
            // position connue du systeme, pendant que le fix precis
            // se calcule en arriere-plan (peut prendre 10-15s).

            DepartureLabel.Text =
                "📍 Récupération du GPS...";

            try
            {
                var quickLocation =
                    await Geolocation.GetLastKnownLocationAsync();

                if (quickLocation != null)
                {
                    Console.WriteLine(
                        $"GPS RAPIDE (cache) : " +
                        $"{quickLocation.Latitude}, {quickLocation.Longitude}");

                    _clientLatitude = quickLocation.Latitude;
                    _clientLongitude = quickLocation.Longitude;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        CenterMapOnLocation(
                            quickLocation.Latitude,
                            quickLocation.Longitude);
                    });

                    DepartureLabel.Text =
                        $"📍 GPS (approx.) : {quickLocation.Latitude:F6}, {quickLocation.Longitude:F6}";
                }
            }
            catch (Exception exQuick)
            {
                Console.WriteLine(
                    $"GPS RAPIDE : indisponible ({exQuick.Message})");
            }

            // ----------------------------------------------------
            // GPS - FIX PRECIS (peut prendre plusieurs secondes)
            // ----------------------------------------------------

            var location =
                await _locationService.GetCurrentLocation();

            if (location == null)
            {
                if (_clientLatitude != 0 && _clientLongitude != 0)
                {
                    // On garde la position approximative deja affichee,
                    // pas la peine de bloquer l'utilisateur.
                    Console.WriteLine(
                        "GPS : fix precis indisponible, position approximative conservee.");
                }
                else
                {
                    DepartureLabel.Text =
                        "❌ Position GPS indisponible.";

                    await DisplayAlert(
                        "GPS",
                        "Impossible de récupérer votre position GPS.",
                        "OK");
                }

                return;
            }

            DepartureLabel.Text =
                $"📍 GPS : {location.Latitude:F6}, {location.Longitude:F6}";

            _clientLatitude = location.Latitude;
            _clientLongitude = location.Longitude;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CenterMapOnLocation(
                    location.Latitude,
                    location.Longitude);
            });

            Console.WriteLine(
                $"GPS CLIENT : " +
                $"{location.Latitude}, " +
                $"{location.Longitude}");

            // ----------------------------------------------------
            // SAUVEGARDE POSITION
            // ----------------------------------------------------

            await _locationService.SaveUserLocationAsync(
                _clientId,
                location.Latitude,
                location.Longitude);

            DepartureLabel.Text =
                $"📍 GPS : {location.Latitude:F6}, {location.Longitude:F6}";
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR ON APPEARING CLIENT : {ex}");

            DepartureLabel.Text =
                "❌ Erreur GPS";

            await DisplayAlert(
                "Erreur",
                ex.ToString(),
                "OK");
        }
    }

    // ============================================================
    // MISE A JOUR POSITION CLIENT
    // ============================================================

    private async Task UpdateClientLocationAsync()
    {
        try
        {
            if (_clientId <= 0)
            {
                Console.WriteLine(
                    "GPS CLIENT : ClientId invalide.");

                return;
            }

            Console.WriteLine(
                $"GPS CLIENT : récupération position " +
                $"pour UserId {_clientId}...");

            var location =
                await _locationService.GetCurrentLocation();

            if (location == null)
            {
                Console.WriteLine(
                    "GPS CLIENT : position introuvable.");

                return;
            }

            Console.WriteLine(
                $"GPS CLIENT ACTUEL : " +
                $"{location.Latitude}, " +
                $"{location.Longitude}");

            await _locationService.SaveUserLocationAsync(
                _clientId,
                location.Latitude,
                location.Longitude);

            Console.WriteLine(
                "GPS CLIENT : position enregistrée.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR UPDATE GPS CLIENT : {ex}");
        }
    }

    // ============================================================
    // REINITIALISER LA PAGE APRES COURSE TERMINEE
    // ============================================================

    public async Task ResetAfterRideCompletedAsync()
    {
        try
        {
            Console.WriteLine(
                "CLIENT : course terminée -> réinitialisation de la page.");

            // ----------------------------------------------------
            // DESTINATION
            // ----------------------------------------------------

            destinationLatitude = 0;
            destinationLongitude = 0;

            DestinationEntry.Text = string.Empty;

            DestinationList.ItemsSource = null;
            DestinationList.IsVisible = false;
            DestinationList.SelectedItem = null;

            // ----------------------------------------------------
            // PRIX
            // ----------------------------------------------------

            PriceLabel.Text =
                "📍 Distance : -- km\n💰 Prix estimé : -- Ar";

            // ----------------------------------------------------
            // STATUT
            // ----------------------------------------------------

            StatusLabel.Text =
                "🚕 Vous pouvez commander une nouvelle course.";

            // ----------------------------------------------------
            // GPS / DEPART
            // ----------------------------------------------------

            DepartureLabel.Text =
                "📍 Récupération du GPS...";

            // ----------------------------------------------------
            // RECHARGER LA SESSION
            // ----------------------------------------------------

            await LoadSessionAsync();

            if (_clientId <= 0)
                return;

            // ----------------------------------------------------
            // GPS ACTUEL
            // ----------------------------------------------------

            var location =
                await _locationService.GetCurrentLocation();

            if (location == null)
            {
                DepartureLabel.Text =
                    "📍 Position actuelle";

                return;
            }

            await _locationService.SaveUserLocationAsync(
                _clientId,
                location.Latitude,
                location.Longitude);

            // ----------------------------------------------------
            // LIEU LE PLUS PROCHE
            // ----------------------------------------------------

            var place =
                await _searchService.GetNearestPlace(
                    location.Latitude,
                    location.Longitude);

            if (place != null)
            {
                DepartureLabel.Text =
                    $"📍 {place.Name}";
            }
            else
            {
                DepartureLabel.Text =
                    "📍 Position actuelle";
            }

            Console.WriteLine(
                "CLIENT : page réinitialisée avec succès.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR RESET CLIENT PAGE : {ex}");

            StatusLabel.Text =
                "❌ Erreur lors de la réinitialisation.";

            await DisplayAlert(
                "Erreur",
                ex.ToString(),
                "OK");
        }
    }

    // ============================================================
    // NETTOYAGE
    // ============================================================

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        try
        {
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
            _searchCancellation = null;

            _rideMonitoringCancellation?.Cancel();
            _rideMonitoringCancellation?.Dispose();
            _rideMonitoringCancellation = null;

            Console.WriteLine(
                "CLIENT HOME PAGE : recherche arrêtée.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR CLEANUP CLIENT HOME : {ex}");
        }
    }

    // ============================================================
    // MONITORING COURSE CLIENT
    // ============================================================

    private void _StartRideMonitoring(int rideId)
    {
        _rideMonitoringCancellation?.Cancel();
        _rideMonitoringCancellation?.Dispose();

        _rideMonitoringCancellation =
            new CancellationTokenSource();

        var token =
            _rideMonitoringCancellation.Token;

        _ = MonitorRideStatusAsync(
            rideId,
            token);
    }

    // ============================================================
    // VERIFIER STATUT COURSE
    // ============================================================

    private async Task MonitorRideStatusAsync(
        int rideId,
        CancellationToken token)
    {
        try
        {
            var rideService =
                _rideService;

            Console.WriteLine(
                $"CLIENT : monitoring course #{rideId} démarré.");

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    token);

                if (token.IsCancellationRequested)
                    return;

                var ride =
                    await rideService.GetRideAsync(
                        rideId);

                if (ride == null)
                    continue;

                Console.WriteLine(
                    $"CLIENT : course #{rideId} => Status = {ride.Status}");
                // ====================================================
                // PAIEMENT MVOLA EN ATTENTE
                // ====================================================

                if (string.Equals(
                        ride.Status,
                        "Accepted",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!_mvolaPaymentPendingShown)
                    {
                        _mvolaPaymentPendingShown = true;

                        Console.WriteLine(
                            $"CLIENT : course #{rideId} acceptée -> paiement MVola en attente.");

                        ShowMvolaPaymentPending();
                    }
                }
                // ====================================================
                // COURSE TERMINEE
                // ====================================================

                if (string.Equals(
                        ride.Status,
                        "Completed",
                        StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(
                        $"CLIENT : COURSE #{rideId} TERMINEE.");

                    _rideMonitoringCancellation?.Cancel();

                    _currentRideId = 0;

                     _mvolaPaymentPendingShown = false;

                    HideMvolaPaymentPending();

                    await MainThread.InvokeOnMainThreadAsync(
                        async () =>
                        {
                            await ResetAfterRideCompletedAsync();
                        });

                    return;
                }
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine(
                $"CLIENT : monitoring course #{rideId} arrêté.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"CLIENT : ERREUR MONITORING #{rideId} : {ex}");
        }
    }

    // ============================================================
    // AFFICHER DEMANDE DE PAIEMENT MVOLA
    // ============================================================

    private void ShowMvolaPaymentPending()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            MvolaPaymentFrame.IsVisible = true;

            StatusLabel.Text =
                "🟡 Paiement MVola en attente";

            Console.WriteLine(
                "CLIENT : paiement MVola en attente.");

            // ----------------------------------------------------
            // VIBRATION - alerte physique meme si l'ecran est en veille visuelle
            // ----------------------------------------------------
            try
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(400));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VIBRATION ERROR : {ex}");
            }

            // ----------------------------------------------------
            // SCROLL AUTOMATIQUE vers le cadre, au cas ou le client
            // serait ailleurs sur la page (le cadre est en bas du ScrollView)
            // ----------------------------------------------------
            try
            {
                await MainScrollView.ScrollToAsync(
                    MvolaPaymentFrame,
                    ScrollToPosition.MakeVisible,
                    animated: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SCROLL ERROR : {ex}");
            }

            // ----------------------------------------------------
            // POPUP - impossible a manquer, contrairement au cadre seul
            // ----------------------------------------------------
            await DisplayAlert(
                "🟡 Paiement MVola en attente",
                "Une demande de paiement MVola vous a été envoyée.\n\n" +
                "Veuillez valider le paiement sur votre téléphone pour confirmer votre course.",
                "OK");
        });
    }

    // ============================================================
    // CACHER DEMANDE DE PAIEMENT MVOLA
    // ============================================================

    private void HideMvolaPaymentPending()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MvolaPaymentFrame.IsVisible = false;

            Console.WriteLine(
                "CLIENT : notification paiement MVola cachée.");
        });
    }

    // ============================================================
    // CARTE (WEBVIEW)
    // ============================================================

    private async Task InitializeMap()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            MapWebView.Navigated += (s, e) =>
            {
                Console.WriteLine("MAP : HTML de base charge (Navigated).");
            };

            // La carte n'est REELLEMENT prete que lorsque le JS le signale
            // lui-meme (apres chargement de Leaflet depuis le CDN et
            // execution de initMap), pas au simple chargement du HTML.
            MapWebView.Navigating += (s, e) =>
            {
                if (e.Url != null && e.Url.Contains("mapready"))
                {
                    e.Cancel = true;
                    _mapReadyTcs.TrySetResult(true);
                    Console.WriteLine("MAP : signal pret recu depuis JS.");
                }
            };

            MapWebView.Source = new HtmlWebViewSource { Html = html };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MAP : erreur chargement map.html : {ex}");
        }
    }

    private async void CenterMapOnLocation(
        double latitude,
        double longitude)
    {
        try
        {
            // Attend que la carte soit prête (max 5s de sécurité)
            var ready = await Task.WhenAny(_mapReadyTcs.Task, Task.Delay(5000));
            if (ready != _mapReadyTcs.Task || !_mapReadyTcs.Task.Result)
            {
                Console.WriteLine("MAP : pas prête, marker ignoré.");
                return;
            }

            var lat = latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lon = longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var result1 = await MapWebView.EvaluateJavaScriptAsync($"centerMap({lat}, {lon}, 15);");
            Console.WriteLine($"MAP JS centerMap result : {result1}");

            var result2 = await MapWebView.EvaluateJavaScriptAsync($"setClientMarker({lat}, {lon});");
            Console.WriteLine($"MAP JS setClientMarker result : {result2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERREUR POSITION MAP : {ex}");
        }
    }

    private async void SetDestinationMarker(
        double latitude,
        double longitude)
    {
        try
        {
            var ready = await Task.WhenAny(_mapReadyTcs.Task, Task.Delay(5000));
            if (ready != _mapReadyTcs.Task || !_mapReadyTcs.Task.Result)
            {
                Console.WriteLine("MAP : pas prête, marker destination ignoré.");
                return;
            }

            var lat = latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lon = longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

            await MapWebView.EvaluateJavaScriptAsync($"setDestinationMarker({lat}, {lon});");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERREUR MARKER DESTINATION : {ex}");
        }
    }

    // ============================================================
    // TRACER LA ROUTE (pickup -> destination) VIA OSRM
    // ============================================================

    private async void ClearDestinationOnMap()
    {
        try
        {
            var ready = await Task.WhenAny(_mapReadyTcs.Task, Task.Delay(3000));
            if (ready != _mapReadyTcs.Task || !_mapReadyTcs.Task.Result)
                return;

            await MapWebView.EvaluateJavaScriptAsync("clearDestinationMarker();");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERREUR CLEAR DESTINATION MAP : {ex}");
        }
    }

    private async void DrawRouteOnMap(
        double pickupLat,
        double pickupLon,
        double destLat,
        double destLon)
    {
        try
        {
            var ready = await Task.WhenAny(_mapReadyTcs.Task, Task.Delay(5000));
            if (ready != _mapReadyTcs.Task || !_mapReadyTcs.Task.Result)
            {
                Console.WriteLine("MAP : pas prête, route ignorée.");
                return;
            }

            var lat1 = pickupLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lon1 = pickupLon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lat2 = destLat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lon2 = destLon.ToString(System.Globalization.CultureInfo.InvariantCulture);

            Console.WriteLine(
                $"MAP : tracé route {lat1},{lon1} -> {lat2},{lon2}");

            var result = await MapWebView.EvaluateJavaScriptAsync(
                $"drawRoute({lat1}, {lon1}, {lat2}, {lon2});");

            Console.WriteLine($"MAP JS drawRoute result : {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERREUR TRACE ROUTE : {ex}");
        }
    }
}
