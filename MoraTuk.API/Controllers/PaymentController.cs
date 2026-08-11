using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.Models;
using MoraTuk.API.Services;

namespace MoraTuk.API.Controllers
{
    [ApiController]
    [Route("api/payment/mvola")]
    public class MvolaController : ControllerBase
    {
        private readonly IMvolaService _mvolaService;
        private readonly AppDbContext _context;

        public MvolaController(
            IMvolaService mvolaService,
            AppDbContext context)
        {
            _mvolaService = mvolaService;
            _context = context;
        }

        // ============================================================
        // POST /api/payment/mvola
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> Pay(
            [FromBody] MvolaPaymentRequest request)
        {
            try
            {
                // ----------------------------------------------------
                // 1. VALIDATION REQUÊTE
                // ----------------------------------------------------

                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "La requête est vide."
                    });
                }

                if (request.RideId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "RideId est obligatoire."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Amount))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Le montant est obligatoire."
                    });
                }

                if (request.DebitParty == null ||
                    request.DebitParty.Count == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "DebitParty est obligatoire."
                    });
                }

                if (request.CreditParty == null ||
                    request.CreditParty.Count == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "CreditParty est obligatoire."
                    });
                }

                // ----------------------------------------------------
                // 2. VÉRIFIER LE MONTANT
                // ----------------------------------------------------

                if (!decimal.TryParse(
                        request.Amount,
                        out decimal amount))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Montant invalide."
                    });
                }

                // ----------------------------------------------------
                // 3. RECHERCHER LA COURSE
                // ----------------------------------------------------

                var ride = await _context.Rides
                    .FirstOrDefaultAsync(
                        x => x.Id == request.RideId);

                if (ride == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Course introuvable."
                    });
                }

                // ----------------------------------------------------
                // 4. VÉRIFIER QUE LE MONTANT CORRESPOND
                // ----------------------------------------------------

                if (amount != ride.Price)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Le montant ne correspond pas au prix de la course.",

                        ridePrice =
                            ride.Price,

                        paymentAmount =
                            amount
                    });
                }

                // ----------------------------------------------------
                // 5. VÉRIFIER SI UN PAIEMENT EXISTE DÉJÀ
                // ----------------------------------------------------

                var existingPayment =
                    await _context.Payments
                        .FirstOrDefaultAsync(
                            x =>
                                x.RideId == ride.Id &&
                                x.Status != "Failed");

                if (existingPayment != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Un paiement existe déjà pour cette course.",

                        paymentId =
                            existingPayment.Id,

                        status =
                            existingPayment.Status
                    });
                }

                // ----------------------------------------------------
                // 6. APPEL MVOLA
                // ----------------------------------------------------

                var result =
                    await _mvolaService.MerchantPayAsync(request);

                Console.WriteLine();
                Console.WriteLine(
                    "========== MVOLA PAYMENT RESULT ==========");
                Console.WriteLine(result);
                Console.WriteLine(
                    "===========================================");

                // ----------------------------------------------------
                // 7. EXTRAIRE LA RÉPONSE MVOLA
                // ----------------------------------------------------

                using var json =
                    System.Text.Json.JsonDocument.Parse(result);

                var root =
                    json.RootElement;

                string? mvolaStatus = null;

                string? serverCorrelationId = null;

                string? transactionReference = null;

                if (root.TryGetProperty(
                        "status",
                        out var statusProperty))
                {
                    mvolaStatus =
                        statusProperty.GetString();
                }

                if (root.TryGetProperty(
                        "serverCorrelationId",
                        out var correlationProperty))
                {
                    serverCorrelationId =
                        correlationProperty.GetString();
                }

                if (root.TryGetProperty(
                        "objectReference",
                        out var objectReferenceProperty))
                {
                    transactionReference =
                        objectReferenceProperty.GetString();
                }

                // ----------------------------------------------------
                // 8. RÉCUPÉRER LES NUMÉROS
                // ----------------------------------------------------

                var debitMsisdn =
                    request.DebitParty
                        .FirstOrDefault(
                            x =>
                                x.Key.Equals(
                                    "msisdn",
                                    StringComparison.OrdinalIgnoreCase))
                        ?.Value;

                var creditMsisdn =
                    request.CreditParty
                        .FirstOrDefault(
                            x =>
                                x.Key.Equals(
                                    "msisdn",
                                    StringComparison.OrdinalIgnoreCase))
                        ?.Value;

                // ----------------------------------------------------
                // 9. CONVERTIR LE STATUT MVOLA
                // ----------------------------------------------------

                string paymentStatus = "Pending";

                if (string.Equals(
                        mvolaStatus,
                        "failed",
                        StringComparison.OrdinalIgnoreCase))
                {
                    paymentStatus = "Failed";
                }
                else if (string.Equals(
                        mvolaStatus,
                        "completed",
                        StringComparison.OrdinalIgnoreCase))
                {
                    paymentStatus = "Completed";
                }
                else if (string.Equals(
                        mvolaStatus,
                        "pending",
                        StringComparison.OrdinalIgnoreCase))
                {
                    paymentStatus = "Pending";
                }

                // ----------------------------------------------------
                // 10. CRÉER PAYMENT
                // ----------------------------------------------------

                var payment = new Payment
                {
                    RideId =
                        ride.Id,

                    Amount =
                        amount,

                    Currency =
                        request.Currency,

                    PaymentMethod =
                        "MVola",

                    ServerCorrelationId =
                        serverCorrelationId,

                    TransactionReference =
                        string.IsNullOrWhiteSpace(
                            transactionReference)
                            ? null
                            : transactionReference,

                    Status =
                        paymentStatus,

                    DebitMsisdn =
                        debitMsisdn,

                    CreditMsisdn =
                        creditMsisdn,

                    Description =
                        request.DescriptionText,

                    CreatedAt =
                        DateTime.UtcNow
                };

                // ----------------------------------------------------
                // 11. ENREGISTRER
                // ----------------------------------------------------

                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();

                Console.WriteLine();
                Console.WriteLine(
                    "========== PAYMENT SAVED ==========");
                Console.WriteLine(
                    $"PaymentId           : {payment.Id}");
                Console.WriteLine(
                    $"RideId              : {payment.RideId}");
                Console.WriteLine(
                    $"Amount              : {payment.Amount}");
                Console.WriteLine(
                    $"Debit               : {payment.DebitMsisdn}");
                Console.WriteLine(
                    $"Credit              : {payment.CreditMsisdn}");
                Console.WriteLine(
                    $"ServerCorrelationId : {payment.ServerCorrelationId}");
                Console.WriteLine(
                    $"Status              : {payment.Status}");
                Console.WriteLine(
                    "===================================");

                // ----------------------------------------------------
                // 12. RÉPONSE
                // ----------------------------------------------------

                return Ok(new
                {
                    success = true,

                    message =
                        "Paiement MVola enregistré.",

                    payment = new
                    {
                        id =
                            payment.Id,

                        rideId =
                            payment.RideId,

                        amount =
                            payment.Amount,

                        currency =
                            payment.Currency,

                        debitMsisdn =
                            payment.DebitMsisdn,

                        creditMsisdn =
                            payment.CreditMsisdn,

                        serverCorrelationId =
                            payment.ServerCorrelationId,

                        transactionReference =
                            payment.TransactionReference,

                        status =
                            payment.Status,

                        createdAt =
                            payment.CreatedAt
                    },

                    mvola =
                        result
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"ERREUR PAYMENT MVOLA : {ex}");

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

        // ============================================================
        // GET /api/payment/mvola/status/{serverCorrelationId}
        // ============================================================

       [HttpGet("status/{serverCorrelationId}")]
        public async Task<IActionResult> GetStatus(
            string serverCorrelationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(serverCorrelationId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "serverCorrelationId est obligatoire."
                    });
                }

                // ========================================================
                // 1. APPEL MVOLA
                // ========================================================

                var result =
                    await _mvolaService.GetPaymentStatusAsync(
                        serverCorrelationId);

                Console.WriteLine();
                Console.WriteLine(
                    "========== UPDATE PAYMENT STATUS ==========");

                Console.WriteLine(
                    $"ServerCorrelationId : {serverCorrelationId}");

                Console.WriteLine(
                    $"MVola Response      : {result}");

                // ========================================================
                // 2. LIRE LA RÉPONSE MVOLA
                // ========================================================

                using var json =
                    System.Text.Json.JsonDocument.Parse(result);

                var root =
                    json.RootElement;

                string? mvolaStatus = null;
                string? transactionReference = null;

                if (root.TryGetProperty(
                        "status",
                        out var statusProperty))
                {
                    mvolaStatus =
                        statusProperty.GetString();
                }

                if (root.TryGetProperty(
                        "objectReference",
                        out var objectReferenceProperty))
                {
                    transactionReference =
                        objectReferenceProperty.GetString();
                }

                // ========================================================
                // 3. CONVERTIR LE STATUT
                // ========================================================

                string paymentStatus;

                if (string.Equals(
                        mvolaStatus,
                        "completed",
                        StringComparison.OrdinalIgnoreCase))
                {
                    paymentStatus = "Completed";
                }
                else if (string.Equals(
                        mvolaStatus,
                        "failed",
                        StringComparison.OrdinalIgnoreCase))
                {
                    paymentStatus = "Failed";
                }
                else
                {
                    paymentStatus = "Pending";
                }

                Console.WriteLine(
                    $"MVola Status       : {mvolaStatus}");

                Console.WriteLine(
                    $"Payment Status     : {paymentStatus}");

                // ========================================================
                // 4. RECHERCHER LE PAYMENT
                // ========================================================

                var payment =
                    await _context.Payments
                        .FirstOrDefaultAsync(
                            x =>
                                x.ServerCorrelationId ==
                                serverCorrelationId);

                if (payment == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Aucun paiement trouvé avec ce serverCorrelationId.",

                        serverCorrelationId
                    });
                }

                // ========================================================
                // 5. METTRE À JOUR LE PAYMENT
                // ========================================================

                payment.Status =
                    paymentStatus;

                payment.UpdatedAt =
                    DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(
                        transactionReference))
                {
                    payment.TransactionReference =
                        transactionReference;
                }

                await _context.SaveChangesAsync();

                Console.WriteLine(
                    $"PaymentId          : {payment.Id}");

                Console.WriteLine(
                    $"Status enregistré   : {payment.Status}");

                Console.WriteLine(
                    "============================================");

                // ========================================================
                // 6. RÉPONSE
                // ========================================================

                return Ok(new
                {
                    success = true,

                    message =
                        "Statut du paiement mis à jour.",

                    payment = new
                    {
                        id = payment.Id,

                        rideId =
                            payment.RideId,

                        amount =
                            payment.Amount,

                        debitMsisdn =
                            payment.DebitMsisdn,

                        creditMsisdn =
                            payment.CreditMsisdn,

                        serverCorrelationId =
                            payment.ServerCorrelationId,

                        transactionReference =
                            payment.TransactionReference,

                        status =
                            payment.Status,

                        updatedAt =
                            payment.UpdatedAt
                    },

                    mvola = result
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"ERREUR UPDATE PAYMENT STATUS : {ex}");

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
// ============================================================
// SYNCHRONISER LE STATUT DU PAIEMENT AVEC MVOLA
// ============================================================

        [HttpPut("sync/{serverCorrelationId}")]
        public async Task<IActionResult> SyncPaymentStatus(
            string serverCorrelationId)
        {
            try
            {
                // --------------------------------------------------------
                // 1. VALIDATION
                // --------------------------------------------------------

                if (string.IsNullOrWhiteSpace(serverCorrelationId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "serverCorrelationId est obligatoire."
                    });
                }

                // --------------------------------------------------------
                // 2. RECHERCHER LE PAYMENT DANS NOTRE BASE
                // --------------------------------------------------------

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(
                        x => x.ServerCorrelationId == serverCorrelationId);

                if (payment == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Paiement introuvable.",
                        serverCorrelationId
                    });
                }

                // --------------------------------------------------------
                // 3. DEMANDER LE STATUT À MVOLA
                // --------------------------------------------------------

                var result =
                    await _mvolaService.GetPaymentStatusAsync(
                        serverCorrelationId);

                Console.WriteLine();
                Console.WriteLine(
                    "========== MVOLA SYNC ==========");

                Console.WriteLine(
                    $"PaymentId           : {payment.Id}");

                Console.WriteLine(
                    $"RideId              : {payment.RideId}");

                Console.WriteLine(
                    $"ServerCorrelationId : {serverCorrelationId}");

                Console.WriteLine(
                    $"MVola Response      : {result}");

                Console.WriteLine(
                    "================================");

                // --------------------------------------------------------
                // 4. LIRE LA RÉPONSE MVOLA
                // --------------------------------------------------------

                using var json =
                    System.Text.Json.JsonDocument.Parse(result);

                var root =
                    json.RootElement;

                string? mvolaStatus = null;

                string? transactionReference = null;

                if (root.TryGetProperty(
                    "status",
                    out var statusProperty))
                {
                    mvolaStatus =
                        statusProperty.GetString();
                }

                if (root.TryGetProperty(
                    "objectReference",
                    out var objectReferenceProperty))
                {
                    transactionReference =
                        objectReferenceProperty.GetString();
                }

                // --------------------------------------------------------
                // 5. CONVERTIR LE STATUT
                // --------------------------------------------------------

                string paymentStatus;

                if (string.Equals(
                    mvolaStatus,
                    "completed",
                    StringComparison.OrdinalIgnoreCase))
                {
                    paymentStatus = "Completed";
                }
                else if (string.Equals(
                    mvolaStatus,
                    "failed",
                    StringComparison.OrdinalIgnoreCase))
                {
                    paymentStatus = "Failed";
                }
                else
                {
                    paymentStatus = "Pending";
                }

                // --------------------------------------------------------
                // 6. METTRE À JOUR PAYMENT
                // --------------------------------------------------------

                payment.Status =
                    paymentStatus;

                if (!string.IsNullOrWhiteSpace(
                    transactionReference))
                {
                    payment.TransactionReference =
                        transactionReference;
                }

                payment.UpdatedAt =
                    DateTime.UtcNow;

                // --------------------------------------------------------
                // 7. SI PAIEMENT TERMINÉ
                // --------------------------------------------------------

                if (paymentStatus == "Completed")
                {
                    var ride = await _context.Rides
                        .FirstOrDefaultAsync(
                            x => x.Id == payment.RideId);

                    if (ride != null)
                    {
                        // Pour l'instant on marque simplement
                        // la course comme payée.

                        ride.Status = "Paid";
                    }
                }

                // --------------------------------------------------------
                // 8. SAUVEGARDER
                // --------------------------------------------------------

                await _context.SaveChangesAsync();

                // --------------------------------------------------------
                // 9. RÉPONSE
                // --------------------------------------------------------

                return Ok(new
                {
                    success = true,

                    message =
                        "Statut du paiement synchronisé.",

                    payment = new
                    {
                        id = payment.Id,

                        rideId = payment.RideId,

                        amount = payment.Amount,

                        debitMsisdn =
                            payment.DebitMsisdn,

                        creditMsisdn =
                            payment.CreditMsisdn,

                        serverCorrelationId =
                            payment.ServerCorrelationId,

                        transactionReference =
                            payment.TransactionReference,

                        status =
                            payment.Status,

                        updatedAt =
                            payment.UpdatedAt
                    },

                    mvola = new
                    {
                        status =
                            mvolaStatus,

                        objectReference =
                            transactionReference
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"ERREUR SYNC PAYMENT MVOLA : {ex}");

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
        // ============================================================
        // GET /api/payment/mvola/{transactionReference}
        // ============================================================

        [HttpGet("{transactionReference}")]
        public async Task<IActionResult> GetTransaction(
            string transactionReference)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(
                        transactionReference))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "transactionReference est obligatoire."
                    });
                }

                var result =
                    await _mvolaService.GetTransactionAsync(
                        transactionReference);

                return Ok(new
                {
                    success = true,
                    data = result
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
