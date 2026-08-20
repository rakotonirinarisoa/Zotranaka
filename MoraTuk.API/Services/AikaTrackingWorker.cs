using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.Hubs;
using MoraTuk.API.Models;
using System.Collections.Concurrent;

namespace MoraTuk.API.Services;

public class AikaTrackingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AikaTrackingWorker> _logger;
    private readonly IConfiguration _configuration;
    private static readonly ConcurrentDictionary<int, double> DriverSpeeds = new();

    public AikaTrackingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AikaTrackingWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "AIKA Tracking Worker démarré.");

        var intervalSeconds =
            _configuration.GetValue<int>(
                "Aika:TrackingIntervalSeconds",
                10);

        if (intervalSeconds < 5)
            intervalSeconds = 5;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SynchronizeDrivers(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erreur pendant la synchronisation AIKA.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(intervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation(
            "AIKA Tracking Worker arrêté.");
    }

    // ============================================================
    // SYNCHRONISER TOUS LES CHAUFFEURS
    // ============================================================

    private async Task SynchronizeDrivers(
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var aikaService =
            scope.ServiceProvider
                .GetRequiredService<AikaLocationService>();

        var hub =
            scope.ServiceProvider
                .GetRequiredService<IHubContext<TrackingHub>>();

        var drivers =
            await context.Drivers
                .Where(x =>
                    x.AikaDeviceId != null &&
                    !string.IsNullOrWhiteSpace(
                        x.AikaUsername) &&
                    !string.IsNullOrWhiteSpace(
                        x.AikaPassword))
                .ToListAsync(cancellationToken);

        if (drivers.Count == 0)
        {
            _logger.LogDebug(
                "Aucun chauffeur avec GPS AIKA configuré.");

            return;
        }

        _logger.LogInformation(
            "AIKA : {Count} chauffeur(s) à synchroniser.",
            drivers.Count);

        // IMPORTANT :
        // Chaque chauffeur possède son propre compte AIKA.
        foreach (var driver in drivers)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await SynchronizeDriver(
                    context,
                    aikaService,
                    hub,
                    driver,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erreur synchronisation AIKA du chauffeur {DriverId}, Device {DeviceId}",
                    driver.Id,
                    driver.AikaDeviceId);
            }
        }
    }

    // ============================================================
    // SYNCHRONISER UN CHAUFFEUR
    // ============================================================

    private async Task SynchronizeDriver(
        AppDbContext context,
        AikaLocationService aikaService,
        IHubContext<TrackingHub> hub,
        Driver driver,
        CancellationToken cancellationToken)
    {
        if (!driver.AikaDeviceId.HasValue)
            return;

        var deviceId =
            driver.AikaDeviceId.Value;

        if (string.IsNullOrWhiteSpace(
                driver.AikaUsername) ||
            string.IsNullOrWhiteSpace(
                driver.AikaPassword))
        {
            _logger.LogWarning(
                "Identifiants AIKA manquants pour Driver={DriverId}, Device={DeviceId}",
                driver.Id,
                deviceId);

            return;
        }

        _logger.LogInformation(
            "AIKA : synchronisation Driver={DriverId}, Device={DeviceId}, Username={Username}",
            driver.Id,
            deviceId,
            driver.AikaUsername);

        // ========================================================
        // APPEL AIKA
        // ========================================================

        var location =
            await aikaService.GetTrackingAsync(
                deviceId,
                driver.AikaUsername,
                driver.AikaPassword);

        if (location == null)
        {
            _logger.LogWarning(
                "AIKA n'a pas retourné de position pour Driver={DriverId}, Device={DeviceId}",
                driver.Id,
                deviceId);

            return;
        }

        if (!location.IsGps)
        {
            _logger.LogWarning(
                "GPS AIKA invalide pour Driver={DriverId}, Device={DeviceId}",
                driver.Id,
                deviceId);

            return;
        }
        // ========================================================
        // SAUVEGARDER LA VITESSE AIKA
        // ========================================================
        DriverSpeeds[driver.Id] = location.Speed;

        // ========================================================
        // SAUVEGARDE POSITION
        // ========================================================

        driver.Latitude =
            location.Latitude;

        driver.Longitude =
            location.Longitude;

        driver.LastUpdate =
            DateTime.UtcNow;

        await context.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "GPS AIKA synchronisé : " +
            "Driver={DriverId}, " +
            "Device={DeviceId}, " +
            "Lat={Latitude}, " +
            "Lng={Longitude}, " +
            "Speed={Speed}",
            driver.Id,
            location.DeviceId,
            location.Latitude,
            location.Longitude,
            location.Speed);

        // ========================================================
        // COURSE ACTIVE
        // ========================================================

        var ride =
            await context.Rides
                .FirstOrDefaultAsync(
                    x =>
                        x.DriverId == driver.Id &&
                        x.Status == "Accepted",
                    cancellationToken);

        if (ride == null)
            return;

        // ========================================================
        // SIGNALR
        // ========================================================

        await hub.Clients
            .Group($"client-{ride.ClientId}")
            .SendAsync(
                "DriverLocation",
                new
                {
                    driverId = driver.Id,
                    latitude = location.Latitude,
                    longitude = location.Longitude,
                    speed = location.Speed,
                    course = location.Course,
                    positionTime = location.PositionTime,
                    gps = location.IsGps,
                    stopped = location.IsStopped
                },
                cancellationToken);
    }
    public static double GetDriverSpeed(int driverId)
    {
        return DriverSpeeds.TryGetValue(
            driverId,
            out var speed)
            ? speed
            : 0;
    }
}