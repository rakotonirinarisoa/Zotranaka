using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.Models;

namespace MoraTuk.API.Controllers;

[ApiController]
[Route("api/driver-payouts")]
public class DriverPayoutController : ControllerBase
{
    private readonly AppDbContext _context;

    public DriverPayoutController(AppDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // PREPARER LE PAYOUT D'UN CHAUFFEUR
    // ============================================================

    [HttpPost("prepare/{driverId}")]
    public async Task<IActionResult> PreparePayout(int driverId)
    {
        try
        {
            // ----------------------------------------------------
            // 1. Vérifier le chauffeur
            // ----------------------------------------------------

            var driver =
                await _context.Drivers
                    .FirstOrDefaultAsync(x => x.Id == driverId);

            if (driver == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Chauffeur introuvable.",
                    driverId
                });
            }

            // ----------------------------------------------------
            // 2. Récupérer les gains prêts à être payés
            // ----------------------------------------------------

            var earnings =
                await _context.DriverEarnings
                    .Where(x =>
                        x.DriverId == driverId &&
                        x.Status == "ReadyForPayout" &&
                        x.DriverPayoutId == null)
                    .ToListAsync();

            // ----------------------------------------------------
            // 3. Aucun gain disponible
            // ----------------------------------------------------

            if (earnings.Count == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "Aucun gain prêt à être payé.",
                    driverId,
                    totalRides = 0,
                    driverAmount = 0m,
                    earnings = Array.Empty<object>()
                });
            }

            // ----------------------------------------------------
            // 4. Calculer les totaux
            // ----------------------------------------------------

            var grossAmount =
                earnings.Sum(x => x.GrossAmount);

            var commissionAmount =
                earnings.Sum(x => x.CommissionAmount);

            var waitingFeeAmount =
                earnings.Sum(x => x.WaitingFeeAmount);

            var driverAmount =
                earnings.Sum(x => x.DriverAmount);

            // ----------------------------------------------------
            // 5. Créer le payout
            // ----------------------------------------------------

            var payout = new DriverPayout
            {
                DriverId = driverId,

                PayoutDate = DateTime.UtcNow.Date,

                TotalRides = earnings.Count,

                GrossAmount = grossAmount,

                CommissionAmount = commissionAmount,

                WaitingFeeAmount = waitingFeeAmount,

                DriverAmount = driverAmount,

                Status = "Pending",

                CreatedAt = DateTime.UtcNow
            };

            _context.DriverPayouts.Add(payout);

            await _context.SaveChangesAsync();

            // ----------------------------------------------------
            // 6. Attacher les earnings au payout
            // ----------------------------------------------------

            foreach (var earning in earnings)
            {
                earning.DriverPayoutId = payout.Id;

                // Le gain est maintenant réservé
                // pour ce payout.
                earning.Status = "Processing";
            }

            await _context.SaveChangesAsync();

            // ----------------------------------------------------
            // 7. Réponse
            // ----------------------------------------------------

            return Ok(new
            {
                success = true,

                message = "Payout préparé avec succès.",

                payout = new
                {
                    id = payout.Id,

                    driverId = payout.DriverId,

                    payoutDate = payout.PayoutDate,

                    totalRides = payout.TotalRides,

                    grossAmount = payout.GrossAmount,

                    commissionAmount =
                        payout.CommissionAmount,

                    waitingFeeAmount =
                        payout.WaitingFeeAmount,

                    driverAmount =
                        payout.DriverAmount,

                    status = payout.Status,

                    createdAt = payout.CreatedAt
                },

                earnings = earnings.Select(x => new
                {
                    id = x.Id,

                    rideId = x.RideId,

                    paymentId = x.PaymentId,

                    grossAmount = x.GrossAmount,

                    commissionAmount =
                        x.CommissionAmount,

                    waitingFeeAmount =
                        x.WaitingFeeAmount,

                    driverAmount =
                        x.DriverAmount,

                    status = x.Status
                })
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR PREPARE PAYOUT : {ex}");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message = ex.Message,
                    innerException =
                        ex.InnerException?.Message
                });
        }
    }
}