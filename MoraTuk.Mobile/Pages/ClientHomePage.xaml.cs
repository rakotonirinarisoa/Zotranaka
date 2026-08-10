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

    private List<LocationDto> _places = new();

    private CancellationTokenSource? _searchToken;
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

        //LoadSession();
    }


private async Task LoadSessionAsync()
{
    try
    {
        var id = await SecureStorage.GetAsync("userId");

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new Exception("Aucun userId trouvé dans la session.");
        }

        if (!int.TryParse(id, out var clientId))
        {
            throw new Exception($"userId invalide : {id}");
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

            if (destinationLatitude == 0 ||
                destinationLongitude == 0)
            {
                await DisplayAlert(
                    "Destination",
                    "Veuillez sélectionner une destination.",
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
            await DisplayAlert(
                "Erreur calcul",
                ex.ToString(),
                "OK");
        }
    }

    private async void SearchDriver_Clicked(
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
                    "Veuillez choisir une destination.",
                    "OK");

                return;
            }

            /*
             * IMPORTANT :
             * On utilise la dernière position enregistrée.
             * On ne redemande PAS le GPS ici.
             */
             await UpdateClientLocationAsync();
            var location = await Geolocation.GetLocationAsync(
                new GeolocationRequest(
                    GeolocationAccuracy.High,
                    TimeSpan.FromSeconds(10)));

            if (location == null)
            {
                await DisplayAlert(
                    "GPS",
                    "Impossible de récupérer votre position GPS.",
                    "OK");

                return;
            }

            Console.WriteLine(
                $"GPS CLIENT AU MOMENT DE LA RÉSERVATION : " +
                $"{location.Latitude}, {location.Longitude}");

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
                    RideTypePicker.SelectedIndex == 1
                        ? "Private"
                        : "Shared",

                Status = "WaitingDriver"
            };

            StatusLabel.Text =
                "Recherche d'un chauffeur...";

            var result =
                await _rideService.CreateRideAsync(dto);

            if (result == null)
            {
                await DisplayAlert(
                    "Erreur",
                    "Impossible de créer la course.",
                    "OK");

                return;
            }

            StatusLabel.Text =
                string.IsNullOrWhiteSpace(result.Driver)
                    ? "Recherche d'un chauffeur..."
                    : $"Chauffeur trouvé : {result.Driver}";

            await DisplayAlert(
                "Course créée",
                $"Prix : {result.Price} Ar\n" +
                $"Statut : {result.Status}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erreur recherche chauffeur",
                ex.ToString(),
                "OK");
        }
    }

    private async void Destination_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                DestinationList.IsVisible = false;
                return;
            }

            _searchCancellation?.Cancel();

            _searchCancellation =
                new CancellationTokenSource();

            await Task.Delay(
                500,
                _searchCancellation.Token);

            var places =
                await _searchService.SearchAsync(
                    e.NewTextValue);

            if (places == null ||
                places.Count == 0)
            {
                DestinationList.IsVisible = false;
                return;
            }

            DestinationList.ItemsSource = null;
            DestinationList.ItemsSource = places;
            DestinationList.IsVisible = true;
        }
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erreur recherche",
                ex.Message,
                "OK");
        }
    }

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

            StatusLabel.Text =
                $"Destination : {place.Name}";

            destinationLatitude =
                place.Latitude;

            destinationLongitude =
                place.Longitude;

            DestinationList.IsVisible = false;
            DestinationList.SelectedItem = null;

            await DisplayAlert(
                "Destination sélectionnée",
                $"{place.Name}\n\n" +
                $"Lat : {place.Latitude}\n" +
                $"Lon : {place.Longitude}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erreur sélection",
                ex.ToString(),
                "OK");
        }
    }

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
                    DestinationEntry.Text + " Toamasina");

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

            await DisplayAlert(
                "Destination sélectionnée",
                $"{place.Name}\n\n" +
                $"Latitude : {destinationLatitude}\n" +
                $"Longitude : {destinationLongitude}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erreur destination",
                ex.ToString(),
                "OK");
        }
    }

    private async void DestinationEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                e.NewTextValue))
            {
                DestinationList.IsVisible = false;
                return;
            }

            var places =
                await _searchService.SearchAsync(
                    e.NewTextValue);

            DestinationList.ItemsSource = places;

            DestinationList.IsVisible =
                places != null &&
                places.Count > 0;
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Erreur recherche",
                ex.ToString(),
                "OK");
        }
    }

    
protected override async void OnAppearing()
{
    base.OnAppearing();

    try
    {
        // =====================================================
        // 1. CHARGER LA SESSION AVANT TOUT
        // =====================================================

        await LoadSessionAsync();

        if (_clientId <= 0)
        {
            DepartureLabel.Text =
                "❌ Client non connecté.";

            return;
        }

        Console.WriteLine(
            $"CLIENT ID : {_clientId}");

        // =====================================================
        // 2. RÉCUPÉRER LE GPS ACTUEL
        // =====================================================

        DepartureLabel.Text =
            "📍 Récupération du GPS...";

        var location =
            await _locationService.GetCurrentLocation();

        if (location == null)
        {
            DepartureLabel.Text =
                "❌ Position GPS indisponible ou imprécise.";

            await DisplayAlert(
                "GPS",
                "Impossible de récupérer votre position GPS.",
                "OK");

            return;
        }

        Console.WriteLine(
            $"GPS CLIENT : " +
            $"{location.Latitude}, {location.Longitude}");

        // =====================================================
        // 3. ENREGISTRER LA POSITION DU CLIENT
        // =====================================================

        await _locationService.SaveUserLocationAsync(
            _clientId,
            location.Latitude,
            location.Longitude);

        Console.WriteLine(
            $"POSITION CLIENT ENREGISTRÉE : {_clientId}");

        // =====================================================
        // 4. AFFICHER LE DÉPART
        // =====================================================

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
            $"GPS CLIENT : récupération position actuelle pour UserId {_clientId}...");

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
            $"{location.Latitude}, {location.Longitude}");

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