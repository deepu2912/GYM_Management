using GymManagement.Api.Data;
using GymManagement.Api.DTOs;
using GymManagement.Api.Entities;
using GymManagement.Api.Services.Email;
using GymManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class GymsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<GymsController> _logger;

    public GymsController(AppDbContext context, IEmailService emailService, ILogger<GymsController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var gyms = _context.GymTenants
            .OrderBy(x => x.GymName)
            .ToList()
            .Select(MapToResponse)
            .ToList();

        return Ok(gyms);
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var gym = _context.GymTenants.FirstOrDefault(x => x.Id == id);
        if (gym is null)
        {
            return NotFound(new { message = "Gym not found." });
        }

        return Ok(MapToResponse(gym));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGymTenantRequest request, CancellationToken cancellationToken)
    {
        var normalizedGymEmail = request.Email.Trim().ToUpperInvariant();
        var normalizedAdminEmail = request.AdminEmail.Trim().ToUpperInvariant();

        if (_context.GymTenants.Any(x => x.Email.ToUpper() == normalizedGymEmail))
        {
            return Conflict(new { message = "A gym with this email already exists." });
        }

        if (_context.Users.Any(x => x.Email.ToUpper() == normalizedAdminEmail))
        {
            return Conflict(new { message = "Admin email is already registered." });
        }

        if (_context.GymTenants.Any(x => x.GymName.ToUpper() == request.GymName.Trim().ToUpperInvariant()))
        {
            return Conflict(new { message = "Gym name already exists." });
        }

        var gym = new GymTenant
        {
            Id = Guid.NewGuid(),
            GymName = request.GymName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            AddressLine = request.AddressLine.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            Pincode = request.Pincode.Trim(),
            GstNumber = request.GstNumber.Trim(),
            BankName = request.BankName.Trim(),
            AccountName = request.AccountName.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            IfscCode = request.IfscCode.Trim(),
            UpiId = string.IsNullOrWhiteSpace(request.UpiId) ? null : request.UpiId.Trim(),
            HsnSacCode = request.HsnSacCode.Trim(),
            GstRatePercent = Math.Clamp(request.GstRatePercent, 0, 100),
            IsGstApplicable = request.IsGstApplicable,
            IsActive = true,
            SubscriptionPlan = "None",
            SubscriptionValidTill = null,
            LifetimePlanActivated = false,
            CreatedOn = DateTime.UtcNow
        };

        var generatedPassword = GenerateSecurePassword();
        var adminUser = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = request.AdminName.Trim(),
            Email = request.AdminEmail.Trim(),
            PasswordHash = PasswordHasher.Hash(generatedPassword),
            MustChangePassword = true,
            Role = AppRole.Admin,
            GymTenantId = gym.Id
        };

        _context.GymTenants.Add(gym);
        _context.Users.Add(adminUser);
        _context.GymProfiles.Add(new GymProfile
        {
            Id = Guid.NewGuid(),
            GymTenantId = gym.Id,
            GymName = gym.GymName,
            Email = gym.Email,
            Phone = gym.Phone,
            AddressLine = gym.AddressLine,
            City = gym.City,
            State = gym.State,
            Pincode = gym.Pincode,
            GstNumber = gym.GstNumber,
            BankName = gym.BankName,
            AccountName = gym.AccountName,
            AccountNumber = gym.AccountNumber,
            IfscCode = gym.IfscCode,
            UpiId = gym.UpiId,
            HsnSacCode = gym.HsnSacCode,
            GstRatePercent = gym.GstRatePercent,
            IsGstApplicable = gym.IsGstApplicable
        });
        _context.SaveChanges();

        var onboardingEmailSent = false;
        string? onboardingEmailError = null;
        var portalUrl = $"{Request.Scheme}://{Request.Host}";

        try
        {
            onboardingEmailSent = await _emailService.SendGymOnboardingAsync(
                adminUser.Email,
                adminUser.Name,
                gym.GymName,
                adminUser.Email,
                generatedPassword,
                portalUrl,
                cancellationToken);
        }
        catch (Exception ex)
        {
            onboardingEmailError = ex.Message;
            _logger.LogError(ex, "Failed to send onboarding email for gym {GymId}.", gym.Id);
        }

        return CreatedAtAction(nameof(GetById), new { id = gym.Id }, new
        {
            gym = MapToResponse(gym),
            onboardingEmailSent,
            onboardingEmailError
        });
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] UpdateGymTenantRequest request)
    {
        var gym = _context.GymTenants.FirstOrDefault(x => x.Id == id);
        if (gym is null)
        {
            return NotFound(new { message = "Gym not found." });
        }

        var adminUser = _context.Users.FirstOrDefault(x => x.GymTenantId == id && x.Role == AppRole.Admin);
        if (adminUser is null)
        {
            return NotFound(new { message = "Gym admin account not found." });
        }

        var normalizedGymEmail = request.Email.Trim().ToUpperInvariant();
        var normalizedAdminEmail = request.AdminEmail.Trim().ToUpperInvariant();
        var normalizedGymName = request.GymName.Trim().ToUpperInvariant();

        if (_context.GymTenants.Any(x => x.Id != id && x.Email.ToUpper() == normalizedGymEmail))
        {
            return Conflict(new { message = "A gym with this email already exists." });
        }

        if (_context.GymTenants.Any(x => x.Id != id && x.GymName.ToUpper() == normalizedGymName))
        {
            return Conflict(new { message = "Gym name already exists." });
        }

        if (_context.Users.Any(x => x.Id != adminUser.Id && x.Email.ToUpper() == normalizedAdminEmail))
        {
            return Conflict(new { message = "Admin email is already registered." });
        }

        gym.GymName = request.GymName.Trim();
        gym.Email = request.Email.Trim();
        gym.Phone = request.Phone.Trim();
        gym.AddressLine = request.AddressLine.Trim();
        gym.City = request.City.Trim();
        gym.State = request.State.Trim();
        gym.Pincode = request.Pincode.Trim();
        gym.GstNumber = request.GstNumber.Trim();
        gym.BankName = request.BankName.Trim();
        gym.AccountName = request.AccountName.Trim();
        gym.AccountNumber = request.AccountNumber.Trim();
        gym.IfscCode = request.IfscCode.Trim();
        gym.UpiId = string.IsNullOrWhiteSpace(request.UpiId) ? null : request.UpiId.Trim();
        gym.HsnSacCode = request.HsnSacCode.Trim();
        gym.GstRatePercent = Math.Clamp(request.GstRatePercent, 0, 100);
        gym.IsGstApplicable = request.IsGstApplicable;
        gym.IsActive = request.IsActive;

        adminUser.Name = request.AdminName.Trim();
        adminUser.Email = request.AdminEmail.Trim();
        if (!string.IsNullOrWhiteSpace(request.AdminPassword))
        {
            adminUser.PasswordHash = PasswordHasher.Hash(request.AdminPassword);
        }

        _context.SaveChanges();
        return Ok(MapToResponse(gym));
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var gym = _context.GymTenants.FirstOrDefault(x => x.Id == id);
        if (gym is null)
        {
            return NotFound(new { message = "Gym not found." });
        }

        var adminUsers = _context.Users.Where(x => x.GymTenantId == id).ToList();
        if (adminUsers.Count > 0)
        {
            _context.Users.RemoveRange(adminUsers);
        }

        _context.GymTenants.Remove(gym);
        _context.SaveChanges();
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public IActionResult UpdateStatus(Guid id, [FromBody] UpdateGymStatusRequest request)
    {
        var gym = _context.GymTenants.FirstOrDefault(x => x.Id == id);
        if (gym is null)
        {
            return NotFound(new { message = "Gym not found." });
        }

        gym.IsActive = request.IsActive;
        _context.SaveChanges();
        return Ok(MapToResponse(gym));
    }

    [HttpGet("{id:guid}/subscription")]
    public IActionResult GetSubscription(Guid id)
    {
        var gym = _context.GymTenants.FirstOrDefault(x => x.Id == id);
        if (gym is null)
        {
            return NotFound(new { message = "Gym not found." });
        }

        return Ok(MapSubscriptionStatus(gym));
    }

    [HttpPost("{id:guid}/subscription/activate")]
    public IActionResult ActivateSubscription(Guid id, [FromBody] ActivateGymSubscriptionRequest request)
    {
        var gym = _context.GymTenants.FirstOrDefault(x => x.Id == id);
        if (gym is null)
        {
            return NotFound(new { message = "Gym not found." });
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

    [HttpPost("{id:guid}/subscription/pay")]
    public async Task<IActionResult> PaySubscription(Guid id, [FromBody] CreateGymSubscriptionPaymentRequest request, CancellationToken cancellationToken)
    {
        var gym = _context.GymTenants.FirstOrDefault(x => x.Id == id);
        if (gym is null)
        {
            return NotFound(new { message = "Gym not found." });
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

    private GymTenantResponse MapToResponse(GymTenant gym)
    {
        var admin = _context.Users
            .Where(x => x.GymTenantId == gym.Id && x.Role == AppRole.Admin)
            .Select(x => new GymAdminResponse
            {
                UserId = x.Id,
                Name = x.Name,
                Email = x.Email
            })
            .FirstOrDefault();

        return new GymTenantResponse
        {
            Id = gym.Id,
            GymName = gym.GymName,
            Email = gym.Email,
            Phone = gym.Phone,
            AddressLine = gym.AddressLine,
            City = gym.City,
            State = gym.State,
            Pincode = gym.Pincode,
            GstNumber = gym.GstNumber,
            BankName = gym.BankName,
            AccountName = gym.AccountName,
            AccountNumber = gym.AccountNumber,
            IfscCode = gym.IfscCode,
            UpiId = gym.UpiId,
            HsnSacCode = gym.HsnSacCode,
            GstRatePercent = gym.GstRatePercent,
            IsGstApplicable = gym.IsGstApplicable,
            IsActive = gym.IsActive,
            SubscriptionPlan = gym.SubscriptionPlan,
            SubscriptionValidTill = gym.SubscriptionValidTill,
            LifetimePlanActivated = gym.LifetimePlanActivated,
            CreatedOn = gym.CreatedOn,
            Admin = admin
        };
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

    private static string GenerateSecurePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string numbers = "23456789";
        const string symbols = "!@#$%&*?";
        const string all = upper + lower + numbers + symbols;

        var chars = new List<char>
        {
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            numbers[RandomNumberGenerator.GetInt32(numbers.Length)],
            symbols[RandomNumberGenerator.GetInt32(symbols.Length)]
        };

        for (var i = chars.Count; i < 12; i++)
        {
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        }

        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }
}
