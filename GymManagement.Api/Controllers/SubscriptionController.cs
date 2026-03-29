using System.Security.Claims;
using GymManagement.Api.Data;
using GymManagement.Api.DTOs;
using GymManagement.Api.Entities;
using GymManagement.Api.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(AppDbContext context, IEmailService emailService, ILogger<SubscriptionController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet("current")]
    public IActionResult GetCurrent()
    {
        var gym = GetCurrentGymTenant();
        if (gym is null)
        {
            return NotFound(new { message = "Gym subscription profile not found." });
        }

        return Ok(MapSubscriptionStatus(gym));
    }

    [HttpPost("activate")]
    [Authorize(Roles = "Admin")]
    public IActionResult Activate([FromBody] ActivateGymSubscriptionRequest request)
    {
        var gym = GetCurrentGymTenant();
        if (gym is null)
        {
            return NotFound(new { message = "Gym subscription profile not found." });
        }

        var plan = GetActivePlan(request.PlanCode);
        if (plan is null)
        {
            return BadRequest(new { message = "Unsupported or inactive plan code." });
        }

        if (!TryApplySubscriptionPlan(gym, plan, out var planError))
        {
            return BadRequest(new { message = planError });
        }

        _context.SaveChanges();
        return Ok(MapSubscriptionStatus(gym));
    }

    [HttpPost("pay")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PaySubscription([FromBody] CreateGymSubscriptionPaymentRequest request, CancellationToken cancellationToken)
    {
        var gym = GetCurrentGymTenant();
        if (gym is null)
        {
            return NotFound(new { message = "Gym subscription profile not found." });
        }

        var plan = GetActivePlan(request.PlanCode);
        if (plan is null)
        {
            return BadRequest(new { message = "Unsupported or inactive plan code." });
        }

        if (request.Amount != plan.Price)
        {
            return BadRequest(new { message = $"Amount mismatch for selected plan. Expected {plan.Price:N2}." });
        }

        if (!TryApplySubscriptionPlan(gym, plan, out var planError))
        {
            return BadRequest(new { message = planError });
        }

        var paidOn = request.PaidOn == default ? DateTime.UtcNow : request.PaidOn;
        var invoiceNumber = GenerateSubscriptionInvoiceNumber();
        var payment = new GymSubscriptionPayment
        {
            Id = Guid.NewGuid(),
            GymTenantId = gym.Id,
            PlanCode = plan.Code,
            Amount = request.Amount,
            PaidOn = paidOn,
            PaymentMode = request.PaymentMode.Trim(),
            TransactionReference = string.IsNullOrWhiteSpace(request.TransactionReference) ? null : request.TransactionReference.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            InvoiceNumber = invoiceNumber,
            CreatedOn = DateTime.UtcNow
        };

        _context.GymSubscriptionPayments.Add(payment);
        _context.SaveChanges();

        var admin = _context.Users.FirstOrDefault(x => x.GymTenantId == gym.Id && x.Role == AppRole.Admin);
        var invoiceEmailSent = false;
        string? invoiceEmailError = null;

        if (admin is not null)
        {
            try
            {
                var pdf = BuildSubscriptionInvoicePdf(gym, payment, plan);
                invoiceEmailSent = await _emailService.SendGymSubscriptionInvoiceAsync(
                    admin.Email,
                    admin.Name,
                    gym.GymName,
                    payment.InvoiceNumber,
                    plan.Name,
                    payment.Amount,
                    payment.PaidOn,
                    payment.PaymentMode,
                    payment.TransactionReference,
                    gym.SubscriptionValidTill,
                    pdf,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                invoiceEmailError = ex.Message;
                _logger.LogError(ex, "Failed to send subscription invoice email for gym {GymId}.", gym.Id);
            }
        }

        return Ok(new GymSubscriptionPaymentResponse
        {
            InvoiceNumber = payment.InvoiceNumber,
            Amount = payment.Amount,
            PaidOn = payment.PaidOn,
            PlanCode = plan.Code,
            ValidTill = gym.SubscriptionValidTill,
            InvoiceEmailSent = invoiceEmailSent,
            InvoiceEmailError = invoiceEmailError
        });
    }

    private GymTenant? GetCurrentGymTenant()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return null;
        }

        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        if (user?.GymTenantId is null)
        {
            return null;
        }

        return _context.GymTenants.FirstOrDefault(x => x.Id == user.GymTenantId.Value);
    }

    private GymSubscriptionStatusResponse MapSubscriptionStatus(GymTenant gym)
    {
        var today = DateTime.UtcNow.Date;
        var validTillDate = gym.SubscriptionValidTill?.Date;
        var daysRemaining = validTillDate.HasValue ? (validTillDate.Value - today).Days : -1;
        var isExpired = !validTillDate.HasValue || validTillDate.Value < today;
        var currentPlan = _context.SubscriptionPlans.FirstOrDefault(x => x.Code == gym.SubscriptionPlan);
        var currentPlanAmount = currentPlan?.Price ?? 0m;
        var currentPlanLabel = string.IsNullOrWhiteSpace(gym.SubscriptionPlan)
            ? "None"
            : currentPlan?.Name ?? gym.SubscriptionPlan;

        return new GymSubscriptionStatusResponse
        {
            Plan = currentPlanLabel,
            ValidTill = gym.SubscriptionValidTill,
            LifetimePlanActivated = gym.LifetimePlanActivated,
            IsExpired = isExpired,
            DaysRemaining = daysRemaining,
            CurrentPlanAmount = currentPlanAmount
        };
    }

    private SubscriptionPlan? GetActivePlan(string planCode)
    {
        var normalizedCode = planCode.Trim().ToUpperInvariant();
        return _context.SubscriptionPlans.FirstOrDefault(x => x.Code == normalizedCode && x.IsActive);
    }

    private static bool TryApplySubscriptionPlan(GymTenant gym, SubscriptionPlan plan, out string? error)
    {
        var now = DateTime.UtcNow.Date;

        if (gym.LifetimePlanActivated && !plan.IsMaintenance)
        {
            error = "Lifetime plan is already active. Only yearly maintenance fee payment is allowed.";
            return false;
        }

        if (plan.IsMaintenance)
        {
            if (!gym.LifetimePlanActivated)
            {
                error = "Maintenance fee is only available for lifetime subscribers.";
                return false;
            }
            if (gym.SubscriptionValidTill.HasValue && gym.SubscriptionValidTill.Value.Date > now.AddDays(30))
            {
                error = "Maintenance payment can be made only when plan is expiring within 30 days or already expired.";
                return false;
            }

            var baseDate = gym.SubscriptionValidTill.HasValue && gym.SubscriptionValidTill.Value.Date > now
                ? gym.SubscriptionValidTill.Value.Date
                : now;
            gym.SubscriptionPlan = "LIFETIME";
            gym.SubscriptionValidTill = baseDate.AddMonths(Math.Max(plan.DurationMonths, 1));
            error = null;
            return true;
        }

        if (plan.IsLifetime)
        {
            if (gym.LifetimePlanActivated)
            {
                error = "Lifetime plan already active. Pay yearly maintenance fee instead.";
                return false;
            }

            gym.SubscriptionPlan = plan.Code;
            gym.SubscriptionValidTill = now.AddMonths(Math.Max(plan.DurationMonths, 12));
            gym.LifetimePlanActivated = true;
            error = null;
            return true;
        }

        gym.SubscriptionPlan = plan.Code;
        gym.SubscriptionValidTill = now.AddMonths(Math.Max(plan.DurationMonths, 1));
        gym.LifetimePlanActivated = false;
        error = null;
        return true;
    }

    private byte[] BuildSubscriptionInvoicePdf(GymTenant gym, GymSubscriptionPayment payment, SubscriptionPlan plan)
    {
        var issuer = _context.SuperAdminInvoiceSettings.FirstOrDefault();
        return Services.Invoice.GymSubscriptionInvoicePdfBuilder.Build(new Services.Invoice.GymSubscriptionInvoiceData
        {
            InvoiceNumber = payment.InvoiceNumber,
            InvoiceDate = payment.PaidOn,
            GymName = gym.GymName,
            GymEmail = gym.Email,
            GymPhone = gym.Phone,
            PlanCode = plan.Code,
            PlanName = plan.Name,
            Amount = payment.Amount,
            PaymentMode = payment.PaymentMode,
            TransactionReference = payment.TransactionReference,
            ValidTill = gym.SubscriptionValidTill,
            IssuerBusinessName = issuer?.BusinessName ?? "ManageMyGym",
            IssuerEmail = issuer?.Email,
            IssuerPhone = issuer?.Phone,
            IssuerAddress = issuer is null ? null : $"{issuer.AddressLine}, {issuer.City}, {issuer.State} - {issuer.Pincode}",
            IssuerGstNumber = issuer?.GstNumber,
            IssuerAuthorizedSignatory = issuer?.AuthorizedSignatory,
            TermsAndConditions = issuer?.TermsAndConditions
        });
    }

    private static string GenerateSubscriptionInvoiceNumber()
    {
        return $"SUBINV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..30];
    }
}
