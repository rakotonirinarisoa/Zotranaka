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

    private double destinationLatitude = 0;
    private double destinationLongitude = 0;

    private CancellationTokenSource? _searchCancellation;

    private int _clientId;

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
    }

    // ============================================================
    // SESSION CLIENT
    // ============================================================

    private async Task LoadSessionAsync()
    {
        try
        {
            var id = await SecureStorage.GetAsync("userId");

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
                $"CLIENT SESSION CHARGÉE : {_clientId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR SESSION CLIENT : {ex}");

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
    // RECHERCHE CHAUFFEUR + CREATION COURSE + MVOLA
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

            // Nettoyage
            mvolaNumber =
                mvolaNumber
                    .Replace(" ", "")
                    .Replace("-", "");

            Console.WriteLine(
                "====================================");

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
                $"{location.Latitude}, {location.Longitude}");

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
            // DTO CREATION COURSE
            // ----------------------------------------------------

            var dto = new CreateRideDto
            {
                ClientId = _clientId,

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
                // NUMERO MVOLA PERSONNEL DU CLIENT
                DebitMsisdn =
                    mvolaNumber
            };

            // ----------------------------------------------------
            // DEBUG
            // ----------------------------------------------------

            Console.WriteLine(
                "========== CREATE RIDE ==========");

            Console.WriteLine(
                $"ClientId       : {dto.ClientId}");

            Console.WriteLine(
                $"DebitMsisdn    : {dto.DebitMsisdn}");

            Console.WriteLine(
                $"Pickup         : " +
                $"{dto.PickupLatitude}, " +
                $"{dto.PickupLongitude}");

            Console.WriteLine(
                $"Destination    : " +
                $"{dto.DestinationLatitude}, " +
                $"{dto.DestinationLongitude}");

            Console.WriteLine(
                $"Departure      : {dto.Departure}");

            Console.WriteLine(
                $"Destination    : {dto.Destination}");

            Console.WriteLine(
                $"RideType       : {dto.RideType}");

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
    // RECHERCHE DESTINATION
    // ============================================================

    private async void DestinationEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        try
        {
            var text =
                e.NewTextValue?.Trim();

            if (string.IsNullOrWhiteSpace(text) ||
                text.Length < 2)
            {
                DestinationList.IsVisible = false;
                return;
            }

            _searchCancellation?.Cancel();

            _searchCancellation =
                new CancellationTokenSource();

            var token =
                _searchCancellation.Token;

            await Task.Delay(
                500,
                token);

            if (token.IsCancellationRequested)
                return;

            Console.WriteLine(
                $"RECHERCHE DESTINATION : {text}");

            var places =
                await _searchService.SearchAsync(
                    text + " Toamasina");

            if (token.IsCancellationRequested)
                return;

            if (places == null ||
                places.Count == 0)
            {
                DestinationList.IsVisible = false;
                return;
            }

            DestinationList.ItemsSource =
                places;

            DestinationList.IsVisible =
                true;
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR RECHERCHE DESTINATION : {ex}");

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
            if (e.CurrentSelection.Count == 0)
                return;

            var place =
                e.CurrentSelection[0] as LocationDto;

            if (place == null)
                return;

            DestinationEntry.Text =
                place.Name;

            destinationLatitude =
                place.Latitude;

            destinationLongitude =
                place.Longitude;

            StatusLabel.Text =
                $"Destination : {place.Name}";

            DestinationList.IsVisible =
                false;

            DestinationList.SelectedItem =
                null;

            await DisplayAlert(
                "Destination sélectionnée",
                $"{place.Name}\n\n" +
                $"Lat : {place.Latitude}\n" +
                $"Lon : {place.Longitude}",
                "OK");
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
            if (string.IsNullOrWhiteSpace(
                DestinationEntry.Text))
            {
                await DisplayAlert(
                    "Destination",
                    "Entrez une destination.",
                    "OK");

                return;
            }

            var places =
                await _searchService.SearchAsync(
                    DestinationEntry.Text.Trim() +
                    " Toamasina");

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
                $"Destination : {place.Name}";

            await DisplayAlert(
                "Destination sélectionnée",
                $"{place.Name}\n\n" +
                $"Latitude : {destinationLatitude}\n" +
                $"Longitude : {destinationLongitude}",
                "OK");
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
            // GPS
            // ----------------------------------------------------

            DepartureLabel.Text =
                "📍 Récupération du GPS...";

            var location =
                await _locationService.GetCurrentLocation();

            if (location == null)
            {
                DepartureLabel.Text =
                    "❌ Position GPS indisponible.";

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

            // ----------------------------------------------------
            // SAUVEGARDE POSITION
            // ----------------------------------------------------

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
}