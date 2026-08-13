using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.Models;
using MoraTuk.API.Services;
using System.Text.Json;

namespace MoraTuk.API.Controllers;

[ApiController]
[Route("api/driver-payouts")]
public class DriverPayoutController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMvolaService _mvolaService;
    private readonly DriverPayoutService _payoutService;

    public DriverPayoutController(
        AppDbContext context,
        IMvolaService mvolaService,
        DriverPayoutService payoutService)
    {
        _context = context;
        _mvolaService = mvolaService;
        _payoutService = payoutService;
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
    [HttpPost("{payoutId}/pay-test")]
    public async Task<IActionResult> PayTest(int payoutId)
    {
        try
        {
            var payout =
                await _context.DriverPayouts
                    .Include(x => x.Driver)
                    .Include(x => x.Earnings)
                    .FirstOrDefaultAsync(x => x.Id == payoutId);

            if (payout == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Payout introuvable."
                });
            }

            // ========================================================
            // PROTECTION DOUBLE PAIEMENT
            // ========================================================

            if (payout.Status == "Paid")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Ce payout est déjà payé.",
                    payoutId
                });
            }

            if (payout.Status != "Pending")
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        $"Le payout ne peut pas être payé. Statut actuel : {payout.Status}",
                    payoutId
                });
            }

            // ========================================================
            // CHAUFFEUR
            // ========================================================

            if (payout.Driver == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Chauffeur introuvable."
                });
            }

            if (string.IsNullOrWhiteSpace(
                payout.Driver.MvolaNumber))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Le chauffeur n'a pas de numéro MVola."
                });
            }

            // ========================================================
            // MONTANT
            // ========================================================

            if (payout.DriverAmount <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Le montant du payout est invalide."
                });
            }

            // ========================================================
            // PASSAGE EN PROCESSING
            // ========================================================

            payout.Status = "Processing";

            foreach (var earning in payout.Earnings)
            {
                earning.Status = "Processing";
            }

            await _context.SaveChangesAsync();

            // ========================================================
            // SIMULATION MVOLA
            // ========================================================

            var reference =
                $"PAYOUT-{payout.Id}-{Guid.NewGuid():N}";

            var result =
                await _mvolaService.TransferToDriverAsync(
                    payout.Driver.MvolaNumber,
                    payout.DriverAmount,
                    $"Paiement ZOTRANAKA - payout #{payout.Id}",
                    reference);

            Console.WriteLine();
            Console.WriteLine(
                "========== DRIVER PAYOUT TEST ==========");

            Console.WriteLine(
                $"Payout ID      : {payout.Id}");

            Console.WriteLine(
                $"Driver ID      : {payout.DriverId}");

            Console.WriteLine(
                $"MVola Number   : {payout.Driver.MvolaNumber}");

            Console.WriteLine(
                $"Amount         : {payout.DriverAmount} Ar");

            Console.WriteLine(
                $"Reference      : {reference}");

            Console.WriteLine(
                $"MVola Response : {result}");

            Console.WriteLine(
                "=========================================");

            // ========================================================
            // LIRE REPONSE MVOLA
            // ========================================================

            using var json =
                JsonDocument.Parse(result);

            var root =
                json.RootElement;

            string? status = null;
            string? transactionReference = null;

            if (root.TryGetProperty(
                "status",
                out var statusProperty))
            {
                status =
                    statusProperty.GetString();
            }

            if (root.TryGetProperty(
                "transactionReference",
                out var transactionProperty))
            {
                transactionReference =
                    transactionProperty.GetString();
            }

            // ========================================================
            // PAIEMENT REUSSI
            // ========================================================

            if (string.Equals(
                status,
                "completed",
                StringComparison.OrdinalIgnoreCase))
            {
                var paidAt =
                    DateTime.UtcNow;

                payout.Status = "Paid";

                payout.PaidAt =
                    paidAt;

                payout.TransactionReference =
                    transactionReference;

                foreach (var earning in payout.Earnings)
                {
                    earning.Status = "Paid";

                    earning.PaidAt =
                        paidAt;

                    earning.PayoutReference =
                        transactionReference;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,

                    message =
                        "Payout effectué avec succès.",

                    payout = new
                    {
                        id = payout.Id,

                        driverId =
                            payout.DriverId,

                        amount =
                            payout.DriverAmount,

                        status =
                            payout.Status,

                        transactionReference =
                            payout.TransactionReference,

                        paidAt =
                            payout.PaidAt
                    },

                    mvola = result
                });
            }

            // ========================================================
            // PAIEMENT ECHOUE
            // ========================================================

            payout.Status = "Failed";

            payout.FailureReason =
                "Le transfert MVola a échoué.";

            foreach (var earning in payout.Earnings)
            {
                earning.Status = "Failed";
            }

            await _context.SaveChangesAsync();

            return BadRequest(new
            {
                success = false,

                message =
                    "Le payout a échoué.",

                payoutId =
                    payout.Id,

                status =
                    payout.Status,

                failureReason =
                    payout.FailureReason,

                mvola = result
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR DRIVER PAYOUT : {ex}");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,

                    message =
                        ex.Message,

                    innerException =
                        ex.InnerException?.Message
                });
        }
    }
    [HttpPost("process-daily/{date}")]
    public async Task<IActionResult> ProcessDaily(string date)
    {
        if (!DateTime.TryParse(date, out var payoutDate))
        {
            return BadRequest(new
            {
                success = false,
                message = "Date invalide. Format attendu : yyyy-MM-dd."
            });
        }

        var result =
            await _payoutService.ProcessDailyPayoutsAsync(
                payoutDate);

        return Ok(result);
    }
}