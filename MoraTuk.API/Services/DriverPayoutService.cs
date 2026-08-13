using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.Models;

namespace MoraTuk.API.Services;

public class DriverPayoutService
{
    private readonly AppDbContext _context;

    public DriverPayoutService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> ProcessDailyPayoutsAsync(DateTime date)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        // Gains prêts à être payés pour la journée
        var earnings = await _context.DriverEarnings
            .Where(x =>
                x.Status == "ReadyForPayout" &&
                x.CreatedAt >= startDate &&
                x.CreatedAt < endDate &&
                x.DriverPayoutId == null)
            .ToListAsync();

        if (earnings.Count == 0)
        {
            return new
            {
                success = true,
                message = "Aucun gain à payer pour cette journée.",
                date = startDate,
                totalDrivers = 0,
                totalRides = 0,
                totalGrossAmount = 0m,
                totalCommissionAmount = 0m,
                totalWaitingFeeAmount = 0m,
                totalDriverAmount = 0m
            };
        }

        // Regrouper par chauffeur
        var groups = earnings
            .GroupBy(x => x.DriverId)
            .ToList();

        var payouts = new List<DriverPayout>();

        foreach (var group in groups)
        {
            var driverId = group.Key;

            var payout = new DriverPayout
            {
                DriverId = driverId,

                PayoutDate = startDate,

                TotalRides = group.Count(),

                GrossAmount =
                    group.Sum(x => x.GrossAmount),

                CommissionAmount =
                    group.Sum(x => x.CommissionAmount),

                WaitingFeeAmount =
                    group.Sum(x => x.WaitingFeeAmount),

                DriverAmount =
                    group.Sum(x => x.DriverAmount),

                Status = "Pending",

                CreatedAt = DateTime.UtcNow
            };

            _context.DriverPayouts.Add(payout);

            payouts.Add(payout);
        }

        // Créer les payouts
        await _context.SaveChangesAsync();

        // Associer chaque earning à son payout
        foreach (var payout in payouts)
        {
            var driverEarnings = earnings
                .Where(x => x.DriverId == payout.DriverId);

            foreach (var earning in driverEarnings)
            {
                earning.DriverPayoutId = payout.Id;
                earning.Status = "Processing";
            }
        }

        await _context.SaveChangesAsync();

        return new
        {
            success = true,

            message =
                "Les payouts quotidiens ont été préparés.",

            date = startDate,

            totalDrivers =
                payouts.Count,

            totalRides =
                payouts.Sum(x => x.TotalRides),

            totalGrossAmount =
                payouts.Sum(x => x.GrossAmount),

            totalCommissionAmount =
                payouts.Sum(x => x.CommissionAmount),

            totalWaitingFeeAmount =
                payouts.Sum(x => x.WaitingFeeAmount),

            totalDriverAmount =
                payouts.Sum(x => x.DriverAmount),

            payouts = payouts.Select(x => new
            {
                id = x.Id,
                driverId = x.DriverId,
                totalRides = x.TotalRides,
                grossAmount = x.GrossAmount,
                commissionAmount = x.CommissionAmount,
                waitingFeeAmount = x.WaitingFeeAmount,
                driverAmount = x.DriverAmount,
                status = x.Status
            })
        };
    }
}