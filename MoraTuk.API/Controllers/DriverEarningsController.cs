using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;

namespace MoraTuk.API.Controllers
{
    [ApiController]
    [Route("api/driver-earnings")]
    public class DriverEarningsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DriverEarningsController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET /api/driver-earnings/driver/{driverId}
        // Regrouper les gains d'un chauffeur
        // ============================================================

        [HttpGet("driver/{driverId}")]
        public async Task<IActionResult> GetDriverEarnings(int driverId)
        {
            try
            {
                if (driverId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "DriverId invalide."
                    });
                }

                // ----------------------------------------------------
                // Vérifier le chauffeur
                // ----------------------------------------------------

                var driverExists =
                    await _context.Drivers
                        .AnyAsync(x => x.Id == driverId);

                if (!driverExists)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Chauffeur introuvable."
                    });
                }

                // ----------------------------------------------------
                // Récupérer les gains
                // ----------------------------------------------------

                var earnings =
                    await _context.DriverEarnings
                        .Where(x =>
                            x.DriverId == driverId &&
                            x.Status != "Paid" &&
                            x.Status != "Failed")
                        .OrderBy(x => x.CreatedAt)
                        .ToListAsync();

                // ----------------------------------------------------
                // Aucun gain
                // ----------------------------------------------------

                if (earnings.Count == 0)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Aucun gain à payer pour ce chauffeur.",
                        driverId,
                        totalRides = 0,
                        grossAmount = 0,
                        commissionAmount = 0,
                        waitingFeeAmount = 0,
                        driverAmount = 0,
                        earnings = new object[] { }
                    });
                }

                // ----------------------------------------------------
                // CALCUL DES TOTAUX
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
                // Réponse
                // ----------------------------------------------------

                return Ok(new
                {
                    success = true,

                    driverId,

                    totalRides =
                        earnings.Count,

                    grossAmount,

                    commissionAmount,

                    waitingFeeAmount,

                    driverAmount,

                    status = "ReadyForPayout",

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

                        status = x.Status,

                        createdAt = x.CreatedAt,

                        paidAt = x.PaidAt,

                        payoutReference =
                            x.PayoutReference
                    })
                });
            }
            catch (Exception ex)
            {
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
        [HttpGet("daily")]
        public async Task<IActionResult> GetDailyEarnings(
            [FromQuery] DateTime? date)
        {
            try
            {
                // Si aucune date n'est fournie :
                // utiliser aujourd'hui en UTC
                var targetDate = (date ?? DateTime.UtcNow).Date;

                var nextDate = targetDate.AddDays(1);

                // ============================================================
                // RÉCUPÉRER LES GAINS DE LA JOURNÉE
                // ============================================================

                var earnings =
                    await _context.DriverEarnings
                        .Where(x =>
                            x.CreatedAt >= targetDate &&
                            x.CreatedAt < nextDate &&
                            x.Status == "ReadyForPayout")
                        .ToListAsync();

                // ============================================================
                // AUCUN GAIN
                // ============================================================

                if (earnings.Count == 0)
                {
                    return Ok(new
                    {
                        success = true,

                        date = targetDate.ToString("yyyy-MM-dd"),

                        totalDrivers = 0,
                        totalRides = 0,

                        totalGrossAmount = 0m,
                        totalCommissionAmount = 0m,
                        totalWaitingFeeAmount = 0m,
                        totalDriverAmount = 0m,

                        drivers = new object[] { }
                    });
                }

                // ============================================================
                // REGROUPEMENT PAR CHAUFFEUR
                // ============================================================

                var drivers =
                    earnings
                        .GroupBy(x => x.DriverId)
                        .Select(group => new
                        {
                            driverId = group.Key,

                            totalRides =
                                group.Count(),

                            grossAmount =
                                group.Sum(x => x.GrossAmount),

                            commissionAmount =
                                group.Sum(x => x.CommissionAmount),

                            waitingFeeAmount =
                                group.Sum(x => x.WaitingFeeAmount),

                            driverAmount =
                                group.Sum(x => x.DriverAmount),

                            status = "ReadyForPayout",

                            earningIds =
                                group.Select(x => x.Id).ToList(),

                            rideIds =
                                group.Select(x => x.RideId).ToList()
                        })
                        .OrderBy(x => x.driverId)
                        .ToList();

                // ============================================================
                // TOTAUX DE LA JOURNÉE
                // ============================================================

                var totalGrossAmount =
                    earnings.Sum(x => x.GrossAmount);

                var totalCommissionAmount =
                    earnings.Sum(x => x.CommissionAmount);

                var totalWaitingFeeAmount =
                    earnings.Sum(x => x.WaitingFeeAmount);

                var totalDriverAmount =
                    earnings.Sum(x => x.DriverAmount);

                // ============================================================
                // RÉPONSE
                // ============================================================

                return Ok(new
                {
                    success = true,

                    date =
                        targetDate.ToString("yyyy-MM-dd"),

                    totalDrivers =
                        drivers.Count,

                    totalRides =
                        earnings.Count,

                    totalGrossAmount,

                    totalCommissionAmount,

                    totalWaitingFeeAmount,

                    totalDriverAmount,

                    drivers
                });
            }
            catch (Exception ex)
            {
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
}