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
    


    // Destination temporaire (test)
    private double destinationLatitude = 0;
    private double destinationLongitude = 0;
    //private List<PlaceResult> _places = new();
    private List<LocationDto> _places = new();
    private CancellationTokenSource? _searchToken;
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
         LoadSession();
    }

private async void LoadSession()
{
    var id = await SecureStorage.GetAsync("userId");

    if(!string.IsNullOrEmpty(id))
    {
        _clientId = int.Parse(id);
    }
}
 private async void CalculatePrice_Clicked(
    object sender,
    EventArgs e)
{
    try
    {
        // Récupérer la dernière position enregistrée du client
        var location =
            await _locationService.GetLastLocationAsync(_clientId);


        if (location == null)
        {
            await DisplayAlert(
                "GPS",
                "Position client introuvable",
                "OK");

            return;
        }


        if(destinationLatitude == 0 ||
           destinationLongitude == 0)
        {
            await DisplayAlert(
                "Destination",
                "Veuillez sélectionner une destination",
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


            // Position réelle depuis UserLocations
            PickupLatitude = location.Latitude,
            PickupLongitude = location.Longitude,
            


            // Destination choisie
            DestinationLatitude = destinationLatitude,
            DestinationLongitude = destinationLongitude,


            Departure =
                DepartureLabel.Text ?? "Position actuelle",


            Destination =
                DestinationEntry.Text ?? "",


            RideType = rideType
        };


        var result =
            await _apiService.EstimatePriceAsync(dto);


        if(result == null)
        {
            await DisplayAlert(
                "Erreur",
                "Impossible de calculer le prix",
                "OK");

            return;
        }


        PriceLabel.Text =
            $"📍 Distance : {result.DistanceKm:F2} km\n" +
            $"💰 Prix estimé : {result.Price:F0} Ar";
    }
    catch(Exception ex)
    {
        await DisplayAlert(
            "Erreur calcul",
            ex.Message,
            "OK");
    }
}

    private async void SearchDriver_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            var userId = await SecureStorage.GetAsync("userId");

            if(destinationLatitude == 0 ||
            destinationLongitude == 0)
            {
                await DisplayAlert(
                    "Destination",
                    "Veuillez choisir une destination",
                    "OK");

                return;
            }
            if (string.IsNullOrEmpty(userId))
            {
                await DisplayAlert(
                    "Erreur",
                    "Utilisateur non connecté",
                    "OK");

                return;
            }


            var location = await _locationService.GetCurrentLocation();


            if(location == null)
            {
                await DisplayAlert(
                    "GPS",
                    "Position introuvable",
                    "OK");

                return;
            }


        //     var distance = _distanceService.Calculate(
        //         location.Latitude,
        //         location.Longitude,
        //         destinationLatitude,
        //         destinationLongitude);


        //    var price = distance * 1500;
        //     // minimum 2100 Ar
        //     if(price < 2100)
        //     {
        //         price = 2100;
        //     }

            await DisplayAlert(
                "Type sélectionné",
                RideTypePicker.SelectedIndex.ToString(),
                "OK");
            var dto = new CreateRideDto
            {
                ClientId = int.Parse(userId),

                Departure = DepartureLabel.Text ?? "Position actuelle",

                PickupLatitude = location.Latitude,
                PickupLongitude = location.Longitude,


                Destination = DestinationEntry.Text ?? "Destination",

                DestinationLatitude = destinationLatitude,
                DestinationLongitude = destinationLongitude,


                //Price = (decimal)price,
                RideType = RideTypePicker.SelectedIndex == 1 
                            ? "Private" 
                            : "Shared",
                

                Status = "WaitingDriver"
            };


            var result = await _rideService.CreateRideAsync(dto);
            StatusLabel.Text =
            $"Chauffeur trouvé : {result.Driver}";

            if(result == null)
            {
                await DisplayAlert(
                    "Erreur",
                    "Impossible de créer la course",
                    "OK");

                return;
            }


            await DisplayAlert(
                "Course créée",
                $"Prix : {result.Price} Ar\nStatut : {result.Status}",
                "OK");


            StatusLabel.Text =
                "Recherche d'un chauffeur...";
        }
        catch(Exception ex)
        {
            await DisplayAlert(
                "Erreur",
                ex.Message,
                "OK");
        }
    }
   
    private CancellationTokenSource? _searchCancellation;
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


            // attendre un peu avant de lancer la recherche
            _searchCancellation?.Cancel();
            _searchCancellation = new CancellationTokenSource();


            await Task.Delay(
                500,
                _searchCancellation.Token);

            var location =
            await _locationService.GetCurrentLocation();

            if(location == null)
            {
                await DisplayAlert(
                    "GPS",
                    "Position introuvable",
                    "OK");

                return;
            }
          var places =
                await _searchService.SearchAsync(
                    DestinationEntry.Text);


            if (places == null || places.Count == 0)
            {
                DestinationList.IsVisible = false;
                return;
            }


            DestinationList.ItemsSource = null;

            DestinationList.ItemsSource = places;

            DestinationList.IsVisible = true;
        }
        catch(TaskCanceledException)
        {
            // normal : nouvelle frappe
        }
        catch(Exception ex)
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
        if(e.CurrentSelection.Count == 0)
            return;


        var place =
            e.CurrentSelection[0] as LocationDto;


        if(place == null)
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
        $"{place.Name}\n\nLat : {place.Latitude}\nLon : {place.Longitude}",
        "OK");


        DestinationList.SelectedItem = null;
    }
    catch(Exception ex)
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
        if (string.IsNullOrWhiteSpace(DestinationEntry.Text))
        {
            await DisplayAlert(
                "Destination",
                "Entrez une destination",
                "OK");

            return;
        }


        var places = await _searchService.SearchAsync(
            DestinationEntry.Text + " Toamasina");


        if (places.Count == 0)
        {
            await DisplayAlert(
                "Destination",
                "Aucun résultat trouvé",
                "OK");

            return;
        }


        var names = places
            .Select(p => p.Name)
            .ToArray();


        var selected = await DisplayActionSheet(
            "Choisir une destination",
            "Annuler",
            null,
            names);


        if (string.IsNullOrEmpty(selected) ||
            selected == "Annuler")
        {
            return;
        }


        var place = places
            .First(p => p.Name == selected);


        destinationLatitude = place.Latitude;


        destinationLongitude = place.Longitude;


        await DisplayAlert(
            "Destination sélectionnée",
            $"{place.Name}\n\n" +
            $"Latitude : {destinationLatitude}\n" +
            $"Longitude : {destinationLongitude}",
            "OK");
    }
   private async void DestinationEntry_TextChanged(
    object sender,
    TextChangedEventArgs e)
{
    try
    {
        if(string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            DestinationList.IsVisible = false;
            return;
        }


        await DisplayAlert(
            "Recherche",
            e.NewTextValue,
            "OK");


        var places =
            await _searchService.SearchAsync(
                e.NewTextValue);


        await DisplayAlert(
            "Résultat",
            $"Nombre : {places.Count}",
            "OK");


        DestinationList.ItemsSource = places;

        DestinationList.IsVisible =
            places.Count > 0;

    }
    catch(Exception ex)
    {
        await DisplayAlert(
            "Erreur recherche",
            ex.ToString(),
            "OK");
    }
}
    // protected override async void OnAppearing()
    // {
    //     base.OnAppearing();

    //     try
    //     {
    //         var location =
    //             await _locationService.GetCurrentLocation();


    //         if(location == null)
    //         {
    //             DepartureLabel.Text =
    //                 "📍 Position introuvable";

    //             return;
    //         }


    //         var place =
    //             await _searchService.GetNearestPlace(
    //                 location.Latitude,
    //                 location.Longitude);

    //          await DisplayAlert(
    //                 "Résultat API Mobile",
    //                 place == null 
    //                 ? "NULL"
    //                 : place.Name,
    //                 "OK");

    //         if(place != null)
    //         {
    //             DepartureLabel.Text =
    //                 $"📍 {place.Name}";
    //         }
    //         else
    //         {
    //              await DisplayAlert(
    //             "API",
    //             "Aucun lieu retourné",
    //             "OK");
    //             DepartureLabel.Text =
    //                 "📍 Position actuelle";
    //             return;
    //         }

    //     }
    //     catch(Exception ex)
    //     {
    //         DepartureLabel.Text =
    //             "📍 Erreur localisation";
    //     }
    // }
protected override async void OnAppearing()
{
    base.OnAppearing();

    try
    {
        DepartureLabel.Text = "Test démarrage";


        if(_locationService == null)
        {
            DepartureLabel.Text = "❌ locationService NULL";
            return;
        }


        if(_searchService == null)
        {
            DepartureLabel.Text = "❌ searchService NULL";
            return;
        }


        var location =
            await _locationService.GetCurrentLocation();


        if(location == null)
        {
            DepartureLabel.Text = "❌ GPS NULL";
            return;
        }
        else
        {
             await _locationService.SaveUserLocationAsync(
                _clientId,
                location.Latitude,
                location.Longitude);
        }


        DepartureLabel.Text =
            $"GPS OK {location.Latitude},{location.Longitude}";


        var place =
            await _searchService.GetNearestPlace(
                location.Latitude,
                location.Longitude);
        await DisplayAlert(
            "API mobile",
            _searchService.LastResponse,
            "OK");

        if(place == null)
        {
            DepartureLabel.Text = "❌ Place NULL";
            return;
        }


        DepartureLabel.Text =
            $"📍 {place.Name}";

    }
    catch(Exception ex)
    {
        DepartureLabel.Text =
            ex.ToString();
    }
}
}