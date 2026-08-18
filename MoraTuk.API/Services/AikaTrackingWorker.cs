using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.Hubs;
using MoraTuk.API.Models;

namespace MoraTuk.API.Services;

public class AikaTrackingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AikaTrackingWorker> _logger;
    private readonly IConfiguration _configuration;

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

        var drivers = await context.Drivers
            .Where(x => x.AikaDeviceId != null)
            .ToListAsync(cancellationToken);

        if (drivers.Count == 0)
        {
            _logger.LogDebug(
                "Aucun chauffeur avec GPS AIKA configuré.");

            return;
        }

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

        _logger.LogDebug(
            "Synchronisation AIKA : Driver={DriverId}, Device={DeviceId}",
            driver.Id,
            deviceId);

        if (string.IsNullOrWhiteSpace(driver.AikaUsername) ||
         string.IsNullOrWhiteSpace(driver.AikaPassword))
        {
            _logger.LogWarning(
                "Identifiants AIKA manquants pour Driver={DriverId}, Device={DeviceId}",
                driver.Id,
                deviceId);

            return;
        }
        var location =
            await aikaService.GetTrackingAsync(
                deviceId,
                driver.AikaUsername!,
                driver.AikaPassword!);

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

        driver.Latitude =
            location.Latitude;

        driver.Longitude =
            location.Longitude;

        driver.LastUpdate =
            DateTime.UtcNow;

        await context.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "GPS AIKA synchronisé : Driver={DriverId}, Lat={Latitude}, Lng={Longitude}, Speed={Speed}",
            driver.Id,
            location.Latitude,
            location.Longitude,
            location.Speed);

        // ========================================================
        // COURSE ACTIVE
        // ========================================================

        var ride = await context.Rides
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
}