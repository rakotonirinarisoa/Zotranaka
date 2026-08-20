using MoraTuk.Mobile.Services;
using System.Globalization;
using System.Text.Json;

namespace MoraTuk.Mobile.Pages;

public partial class OwnerHomePage : ContentPage
{
    private readonly ApiService _apiService;
    

    private readonly TaskCompletionSource<bool> _mapReadyTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public OwnerHomePage(ApiService apiService)
    {
        InitializeComponent();

        _apiService = apiService;

        _ = InitializeMap();
    }
    private CancellationTokenSource? _fleetRefreshCts;
    // ============================================================
    // INITIALISATION CARTE
    // ============================================================

    private async Task InitializeMap()
    {
        try
        {
            using var stream =
                await FileSystem.OpenAppPackageFileAsync("map.html");

            using var reader =
                new StreamReader(stream);

            var html =
                await reader.ReadToEndAsync();

            // ========================================================
            // SIGNAL CARTE PRETE
            // ========================================================

            MapWebView.Navigating += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Url))
                    return;

                Console.WriteLine(
                    $"OWNER MAP NAVIGATING : {e.Url}");

                if (e.Url.Contains("mapready"))
                {
                    e.Cancel = true;

                    _mapReadyTcs.TrySetResult(true);

                    Console.WriteLine(
                        "OWNER MAP : carte prête.");
                }
            };

            // ========================================================
            // CARTE CHARGEE
            // ========================================================

            MapWebView.Navigated += (s, e) =>
            {
                Console.WriteLine(
                    $"OWNER MAP : map.html chargé : {e.Url}");
            };

            // ========================================================
            // CHARGEMENT HTML
            // ========================================================

            MapWebView.Source =
                new HtmlWebViewSource
                {
                    Html = html
                };

            Console.WriteLine(
                "OWNER MAP : HTML envoyé au WebView.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"OWNER MAP ERROR : {ex}");
        }
    }

    // ============================================================
    // ATTENDRE CARTE
    // ============================================================

    private async Task<bool> WaitForMapAsync()
    {
        try
        {
            var completed =
                await Task.WhenAny(
                    _mapReadyTcs.Task,
                    Task.Delay(10000));

            if (completed != _mapReadyTcs.Task)
            {
                Console.WriteLine(
                    "OWNER MAP : timeout attente carte.");

                return false;
            }

            var ready =
                await _mapReadyTcs.Task;

            Console.WriteLine(
                $"OWNER MAP : ready = {ready}");

            return ready;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"OWNER MAP WAIT ERROR : {ex}");

            return false;
        }
    }

    // ============================================================
    // APPARITION PAGE
    // ============================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _fleetRefreshCts?.Cancel();
        _fleetRefreshCts = new CancellationTokenSource();

        await LoadFleetAsync();

        _ = RefreshFleetLoopAsync(_fleetRefreshCts.Token);
    }

    private async Task RefreshFleetLoopAsync(
    CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(120),
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                await LoadFleetAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal lorsque la page disparaît
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"OWNER AUTO REFRESH ERROR : {ex}");
        }
    }

    protected override void OnDisappearing()
    {
        _fleetRefreshCts?.Cancel();
        _fleetRefreshCts?.Dispose();
        _fleetRefreshCts = null;

        base.OnDisappearing();
    }

    // ============================================================
    // CHARGER LA FLOTTE
    // ============================================================

    private async Task LoadFleetAsync()
    {
        try
        {
            LoadingOverlay.IsVisible = true;

            Console.WriteLine(
                "OWNER : récupération de la flotte...");

            // ========================================================
            // API
            // ========================================================

            var vehicles =
                await _apiService.GetFleetLocationsAsync();

            Console.WriteLine(
                $"OWNER : {vehicles.Count} véhicules reçus.");

            FleetCountLabel.Text =
                $"Véhicules : {vehicles.Count}";

            if (vehicles.Count == 0)
            {
                Console.WriteLine(
                    "OWNER : aucun véhicule.");

                return;
            }

            // ========================================================
            // ATTENDRE CARTE
            // ========================================================

            var mapReady =
                await WaitForMapAsync();

            if (!mapReady)
            {
                Console.WriteLine(
                    "OWNER : impossible d'utiliser la carte.");

                return;
            }

            // ========================================================
            // CENTRER SUR LE PREMIER VEHICULE
            // ========================================================

            var first =
                vehicles[0];

            if (first.Latitude != 0 &&
                first.Longitude != 0)
            {
                await CenterMapAsync(
                    first.Latitude,
                    first.Longitude);
            }

            // ========================================================
            // SERIALISATION FLOTTE
            // ========================================================

            var json =
                JsonSerializer.Serialize(
                    vehicles,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy =
                            JsonNamingPolicy.CamelCase
                    });

            Console.WriteLine(
                $"OWNER MAP : flotte JSON : {json}");

            // ========================================================
            // ENVOYER LA FLOTTE À JAVASCRIPT
            // ========================================================

            var javascript =
                $"setVehicles({json});";

            Console.WriteLine(
                $"OWNER MAP JS : {javascript}");

            await MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    await MapWebView.EvaluateJavaScriptAsync(
                        javascript);
                });

            Console.WriteLine(
                "OWNER MAP : setVehicles exécuté.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"OWNER FLEET ERROR : {ex}");

            FleetCountLabel.Text =
                "Erreur chargement flotte";

            await DisplayAlert(
                "Erreur flotte",
                ex.Message,
                "OK");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    // ============================================================
    // ACTUALISER
    // ============================================================

    private async void RefreshButton_Clicked(
        object sender,
        EventArgs e)
    {
        await LoadFleetAsync();
    }

    // ============================================================
    // CENTRER LA CARTE
    // ============================================================

    private async Task CenterMapAsync(
        double latitude,
        double longitude)
    {
        try
        {
            if (!_mapReadyTcs.Task.IsCompleted)
            {
                var ready =
                    await WaitForMapAsync();

                if (!ready)
                    return;
            }

            var lat =
                latitude.ToString(
                    CultureInfo.InvariantCulture);

            var lon =
                longitude.ToString(
                    CultureInfo.InvariantCulture);

            var javascript =
                $"centerMap({lat}, {lon}, 13);";

            Console.WriteLine(
                $"OWNER MAP CENTER JS : {javascript}");

            await MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    await MapWebView.EvaluateJavaScriptAsync(
                        javascript);
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"OWNER MAP CENTER ERROR : {ex}");
        }
    }
}