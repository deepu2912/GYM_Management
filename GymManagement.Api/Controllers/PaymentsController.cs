using System.Security.Claims;
using System.Text.Json;
using GymManagement.Api.Services.Email;
using GymManagement.Api.Data;
using GymManagement.Api.DTOs;
using GymManagement.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        AppDbContext context,
        IEmailService emailService,
        ILogger<PaymentsController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        var member = _context.Members.FirstOrDefault(m => m.Id == request.MemberId);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        decimal? remainingDue = null;
        decimal? dueAmountAfterPayment = null;
        string invoiceNumber;
        string? planName = null;
        byte[]? receiptPdf = null;

        if (request.MemberMembershipId.HasValue)
        {
            var membership = _context.MemberMemberships.FirstOrDefault(mm =>
                mm.Id == request.MemberMembershipId.Value && mm.MemberId == request.MemberId);
            if (membership is null)
            {
                return BadRequest(new { message = "Invalid member membership reference." });
            }

            var plan = _context.MembershipPlans.FirstOrDefault(mp => mp.Id == membership.MembershipPlanId);
            if (plan is null)
            {
                return NotFound(new { message = "Membership plan not found." });
            }

            var planAmount = membership.PlanPriceAtEnrollment > 0m ? membership.PlanPriceAtEnrollment : plan.Price;
            var netPlanAmount = Math.Max(planAmount - membership.Discount, 0m);
            var collectedAmount = _context.Payments
                .Where(p => p.MemberMembershipId == request.MemberMembershipId.Value)
                .Sum(p => (decimal?)p.Amount) ?? 0m;

            remainingDue = Math.Max(netPlanAmount - collectedAmount, 0m);
            if (remainingDue <= 0m)
            {
                return BadRequest(new { message = "Payment is already fully collected for this membership link." });
            }

            if (request.Amount > remainingDue.Value)
            {
                return BadRequest(new
                {
                    message = $"Amount cannot be greater than remaining due (INR {remainingDue.Value:N2})."
                });
            }

            dueAmountAfterPayment = Math.Max(remainingDue.Value - request.Amount, 0m);
            if (string.IsNullOrWhiteSpace(membership.MasterInvoiceNumber))
            {
                membership.MasterInvoiceNumber = GenerateInvoiceNumber();
            }

            invoiceNumber = membership.MasterInvoiceNumber!;
            planName = plan.PlanName;
        }
        else
        {
            invoiceNumber = GenerateInvoiceNumber();
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            MemberId = request.MemberId,
            MemberMembershipId = request.MemberMembershipId,
            ReceiptNumber = GenerateReceiptNumber(),
            Amount = request.Amount,
            PaidOn = request.PaidOn,
            PaymentMode = request.PaymentMode,
            TransactionReference = request.TransactionReference?.Trim(),
            Notes = request.Notes?.Trim(),
            InvoiceNumber = invoiceNumber
        };

        _context.Payments.Add(payment);
        _context.SaveChanges();

        bool receiptEmailSent = false;
        string? receiptEmailError = null;
        bool invoiceEmailSent = false;
        string? invoiceEmailError = null;

        var invoiceReady = !request.MemberMembershipId.HasValue || dueAmountAfterPayment.GetValueOrDefault() <= 0m;

        try
        {
            var gym = _context.GymProfiles.FirstOrDefault() ?? BuildFallbackGymProfile();
            planName ??= GetPlanName(payment.MemberMembershipId);
            var receiptData = BuildReceiptData(payment, member, gym, planName);
            receiptPdf = BuildReceiptPdf(receiptData);
            receiptEmailSent = await _emailService.SendPaymentReceiptAsync(
                member.Email,
                member.Name,
                payment.ReceiptNumber,
                receiptPdf,
                cancellationToken);

            if (invoiceReady)
            {
                var invoiceData = EnsureInvoiceSnapshot(payment, member, gym, planName);
                var pdf = BuildInvoicePdf(invoiceData);
                invoiceEmailSent = await _emailService.SendPaymentInvoiceAsync(
                    member.Email,
                    member.Name,
                    payment.InvoiceNumber,
                    pdf,
                    cancellationToken);
            }
            else
            {
                invoiceEmailError = $"Invoice will be generated after full due is cleared. Remaining due: INR {dueAmountAfterPayment:N2}.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invoice email for payment {PaymentId}.", payment.Id);
            if (!receiptEmailSent)
            {
                receiptEmailError = ex.Message;
            }
            else
            {
                invoiceEmailError = ex.Message;
            }
        }

        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, new
        {
            Payment = ToPaymentResponse(payment),
            InvoiceNumber = payment.InvoiceNumber,
            ReceiptNumber = payment.ReceiptNumber,
            InvoiceReady = invoiceReady,
            RemainingDue = dueAmountAfterPayment,
            ReceiptEmailSent = receiptEmailSent,
            ReceiptEmailError = receiptEmailError,
            InvoiceEmailSent = invoiceEmailSent,
            InvoiceEmailError = invoiceEmailError
        });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Member")]
    public IActionResult GetById(Guid id)
    {
        var payment = _context.Payments.FirstOrDefault(p => p.Id == id);
        if (payment is null)
        {
            return NotFound(new { message = "Payment not found." });
        }

        if (User.IsInRole("Member") && !CanAccessMemberData(payment.MemberId))
        {
            return Forbid();
        }

        return Ok(ToPaymentResponse(payment));
    }

    [HttpGet("member/{memberId:guid}")]
    [Authorize(Roles = "Admin,Member")]
    public IActionResult GetByMember(Guid memberId)
    {
        if (User.IsInRole("Member") && !CanAccessMemberData(memberId))
        {
            return Forbid();
        }

        var payments = _context.Payments
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.PaidOn)
            .ToList();

        return Ok(payments.Select(ToPaymentResponse).ToList());
    }

    [HttpGet("membermembership/{memberMembershipId:guid}")]
    [Authorize(Roles = "Admin,Trainer")]
    public IActionResult GetByMemberMembership(Guid memberMembershipId)
    {
        var exists = _context.MemberMemberships.Any(mm => mm.Id == memberMembershipId);
        if (!exists)
        {
            return NotFound(new { message = "Member membership not found." });
        }

        var payments = _context.Payments
            .Where(p => p.MemberMembershipId == memberMembershipId)
            .OrderByDescending(p => p.PaidOn)
            .ToList()
            .Select(ToPaymentResponse)
            .ToList();

        return Ok(payments);
    }

    [HttpGet("membermembership/{memberMembershipId:guid}/summary")]
    [Authorize(Roles = "Admin,Trainer")]
    public IActionResult GetMemberMembershipSummary(Guid memberMembershipId)
    {
        var membership = _context.MemberMemberships.FirstOrDefault(mm => mm.Id == memberMembershipId);
        if (membership is null)
        {
            return NotFound(new { message = "Member membership not found." });
        }

        var member = _context.Members.FirstOrDefault(m => m.Id == membership.MemberId);
        var plan = _context.MembershipPlans.FirstOrDefault(mp => mp.Id == membership.MembershipPlanId);
        if (member is null || plan is null)
        {
            return NotFound(new { message = "Member or plan not found for this membership." });
        }

        var collectedAmount = _context.Payments
            .Where(p => p.MemberMembershipId == memberMembershipId)
            .Sum(p => (decimal?)p.Amount) ?? 0m;

        var summary = new MemberMembershipPaymentSummaryResponse
        {
            MemberMembershipId = memberMembershipId,
            MemberId = member.Id,
            MemberName = member.Name,
            MemberEmail = member.Email,
            PlanName = plan.PlanName,
            PlanAmount = Math.Max((membership.PlanPriceAtEnrollment > 0m ? membership.PlanPriceAtEnrollment : plan.Price) - membership.Discount, 0m),
            CollectedAmount = collectedAmount,
            DueAmount = Math.Max(Math.Max((membership.PlanPriceAtEnrollment > 0m ? membership.PlanPriceAtEnrollment : plan.Price) - membership.Discount, 0m) - collectedAmount, 0)
        };

        return Ok(summary);
    }

    [HttpGet("{paymentId:guid}/invoice")]
    [Authorize(Roles = "Admin,Member")]
    public IActionResult GenerateInvoice(Guid paymentId)
    {
        var payment = _context.Payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment is null)
        {
            return NotFound(new { message = "Payment not found." });
        }

        var member = _context.Members.FirstOrDefault(m => m.Id == payment.MemberId);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        if (User.IsInRole("Member") && !CanAccessMemberData(member.Id))
        {
            return Forbid();
        }

        if (payment.MemberMembershipId.HasValue)
        {
            var due = GetDueForMemberMembership(payment.MemberMembershipId.Value);
            if (due > 0)
            {
                return BadRequest(new
                {
                    message = $"Invoice is available only after full payment collection. Remaining due: INR {due:N2}."
                });
            }
        }

        var planName = payment.MemberMembershipId.HasValue
            ? (from mm in _context.MemberMemberships
               join mp in _context.MembershipPlans on mm.MembershipPlanId equals mp.Id
               where mm.Id == payment.MemberMembershipId.Value
               select mp.PlanName).FirstOrDefault()
            : null;

        decimal billedAmount = payment.Amount;
        if (payment.MemberMembershipId.HasValue)
        {
            var financials = GetMemberMembershipFinancials(payment.MemberMembershipId.Value);
            billedAmount = financials.NetPlanAmount;
        }

        var gym = _context.GymProfiles.FirstOrDefault() ?? BuildFallbackGymProfile();
        var invoiceData = EnsureInvoiceSnapshot(payment, member, gym, planName, billedAmount);

        var invoice = new
        {
            invoiceData.InvoiceNumber,
            invoiceData.InvoiceDate,
            invoiceData.MemberName,
            invoiceData.MemberEmail,
            invoiceData.MemberPhone,
            invoiceData.PlanName,
            invoiceData.PlanDescription,
            invoiceData.PlanStartDate,
            invoiceData.PlanEndDate,
            invoiceData.HsnSacCode,
            invoiceData.BaseAmountBeforeDiscount,
            invoiceData.DiscountAmount,
            invoiceData.DiscountPercent,
            invoiceData.IsGstApplicable,
            invoiceData.GstRatePercent,
            invoiceData.TaxableAmount,
            invoiceData.CgstAmount,
            invoiceData.SgstAmount,
            invoiceData.GstAmount,
            invoiceData.TotalInvoiceAmount,
            invoiceData.TotalCollected,
            invoiceData.TotalDue,
            invoiceData.AmountInWords,
            Receipts = invoiceData.Receipts.Select(r => new
            {
                r.ReceiptNumber,
                r.PaidOn,
                r.PaymentMode,
                r.Amount
            })
        };

        return Ok(invoice);
    }

    [HttpGet("{paymentId:guid}/invoice/pdf")]
    [Authorize(Roles = "Admin,Member")]
    public IActionResult GenerateInvoicePdf(Guid paymentId)
    {
        var payment = _context.Payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment is null)
        {
            return NotFound(new { message = "Payment not found." });
        }

        var member = _context.Members.FirstOrDefault(m => m.Id == payment.MemberId);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        if (User.IsInRole("Member") && !CanAccessMemberData(member.Id))
        {
            return Forbid();
        }

        if (payment.MemberMembershipId.HasValue)
        {
            var due = GetDueForMemberMembership(payment.MemberMembershipId.Value);
            if (due > 0)
            {
                return BadRequest(new
                {
                    message = $"Invoice PDF is available only after full payment collection. Remaining due: INR {due:N2}."
                });
            }
        }

        var gym = _context.GymProfiles.FirstOrDefault() ?? BuildFallbackGymProfile();

        var planName = GetPlanName(payment.MemberMembershipId) ?? "N/A";
        decimal billedAmount = payment.Amount;
        if (payment.MemberMembershipId.HasValue)
        {
            var financials = GetMemberMembershipFinancials(payment.MemberMembershipId.Value);
            billedAmount = financials.NetPlanAmount;
        }
        var invoiceData = EnsureInvoiceSnapshot(payment, member, gym, planName, billedAmount);
        var pdf = BuildInvoicePdf(invoiceData);

        return File(pdf, "application/pdf", $"{payment.InvoiceNumber}.pdf");
    }

    [HttpPost("{paymentId:guid}/send-invoice-email")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendInvoiceEmail(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = _context.Payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment is null)
        {
            return NotFound(new { message = "Payment not found." });
        }

        var member = _context.Members.FirstOrDefault(m => m.Id == payment.MemberId);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        if (payment.MemberMembershipId.HasValue)
        {
            var due = GetDueForMemberMembership(payment.MemberMembershipId.Value);
            if (due > 0)
            {
                return BadRequest(new
                {
                    message = $"Invoice email is available only after full payment collection. Remaining due: INR {due:N2}."
                });
            }
        }

        try
        {
            var gym = _context.GymProfiles.FirstOrDefault() ?? BuildFallbackGymProfile();
            var planName = GetPlanName(payment.MemberMembershipId);
            decimal billedAmount = payment.Amount;
            if (payment.MemberMembershipId.HasValue)
            {
                var financials = GetMemberMembershipFinancials(payment.MemberMembershipId.Value);
                billedAmount = financials.NetPlanAmount;
            }
            var invoiceData = EnsureInvoiceSnapshot(payment, member, gym, planName, billedAmount);
            var pdf = BuildInvoicePdf(invoiceData);
            var sent = await _emailService.SendPaymentInvoiceAsync(
                member.Email,
                member.Name,
                payment.InvoiceNumber,
                pdf,
                cancellationToken);
            return Ok(new { sent });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invoice email for payment {PaymentId}.", paymentId);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("membermembership/{memberMembershipId:guid}/send-reminder")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendReminder(
        Guid memberMembershipId,
        [FromBody] SendReminderRequest request,
        CancellationToken cancellationToken)
    {
        var membership = _context.MemberMemberships.FirstOrDefault(mm => mm.Id == memberMembershipId);
        if (membership is null)
        {
            return NotFound(new { message = "Member membership not found." });
        }

        var member = _context.Members.FirstOrDefault(m => m.Id == membership.MemberId);
        var plan = _context.MembershipPlans.FirstOrDefault(mp => mp.Id == membership.MembershipPlanId);
        if (member is null || plan is null)
        {
            return NotFound(new { message = "Member or plan not found for this membership." });
        }

        var collectedAmount = _context.Payments
            .Where(p => p.MemberMembershipId == memberMembershipId)
            .Sum(p => (decimal?)p.Amount) ?? 0m;

        var planAmount = membership.PlanPriceAtEnrollment > 0m ? membership.PlanPriceAtEnrollment : plan.Price;
        var netPlanAmount = Math.Max(planAmount - membership.Discount, 0m);
        var dueAmount = Math.Max(netPlanAmount - collectedAmount, 0m);
        if (dueAmount <= 0)
        {
            return BadRequest(new { message = "No due amount available for reminder." });
        }

        try
        {
            var sent = await _emailService.SendPaymentReminderAsync(
                member.Email,
                member.Name,
                plan.PlanName,
                dueAmount,
                request?.Notes?.Trim(),
                cancellationToken);
            return Ok(new { sent, dueAmount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reminder email for memberMembership {MemberMembershipId}.", memberMembershipId);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("due-report")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetDuePaymentsReport()
    {
        var report = _context.Members
            .Select(member => new
            {
                MemberId = member.Id,
                member.Name,
                member.Email,
                TotalMembershipCharges = _context.MemberMemberships
                    .Where(mm => mm.MemberId == member.Id)
                    .Select(mm => (decimal?)(mm.PlanPriceAtEnrollment - mm.Discount))
                    .Sum() ?? 0m,
                TotalPaid = _context.Payments
                    .Where(p => p.MemberId == member.Id)
                    .Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .AsEnumerable()
            .Select(x => new
            {
                x.MemberId,
                x.Name,
                x.Email,
                x.TotalMembershipCharges,
                x.TotalPaid,
                DueAmount = x.TotalMembershipCharges - x.TotalPaid
            })
            .Where(x => x.DueAmount > 0)
            .OrderByDescending(x => x.DueAmount)
            .ToList();

        return Ok(report);
    }

    [HttpGet("member/{memberId:guid}/due")]
    [Authorize(Roles = "Admin,Member")]
    public IActionResult GetMemberDue(Guid memberId)
    {
        if (User.IsInRole("Member") && !CanAccessMemberData(memberId))
        {
            return Forbid();
        }

        var member = _context.Members.FirstOrDefault(m => m.Id == memberId);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        var totalCharges = _context.MemberMemberships
            .Where(mm => mm.MemberId == memberId)
            .Select(mm => (decimal?)(mm.PlanPriceAtEnrollment - mm.Discount))
            .Sum() ?? 0m;

        var totalPaid = _context.Payments
            .Where(p => p.MemberId == memberId)
            .Sum(p => (decimal?)p.Amount) ?? 0m;

        return Ok(new
        {
            MemberId = member.Id,
            member.Name,
            TotalMembershipCharges = totalCharges,
            TotalPaid = totalPaid,
            DueAmount = totalCharges - totalPaid
        });
    }

    [HttpGet("dashboard-summary")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetDashboardSummary()
    {
        EnsureInvoiceSnapshotsForHistoricalPayments();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);
        var today = DateOnly.FromDateTime(now);
        var endingSoonTill = today.AddDays(7);

        var monthlyRevenue = _context.Payments
            .Where(p =>
                p.MemberMembershipId.HasValue &&
                p.PaidOn >= monthStart &&
                p.PaidOn < nextMonthStart)
            .Sum(p => (decimal?)p.Amount) ?? 0m;

        var pendingCollectionAmount = _context.MemberMemberships
            .Select(mm => new
            {
                NetPlanAmount = mm.PlanPriceAtEnrollment - mm.Discount,
                CollectedAmount = _context.Payments
                    .Where(p => p.MemberMembershipId == mm.Id)
                    .Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .AsEnumerable()
            .Sum(x => Math.Max(Math.Max(x.NetPlanAmount, 0m) - x.CollectedAmount, 0m));

        var trend = Enumerable.Range(0, 6)
            .Select(index =>
            {
                var start = monthStart.AddMonths(index - 5);
                var end = start.AddMonths(1);

                var revenue = _context.Payments
                    .Where(p =>
                        p.MemberMembershipId.HasValue &&
                        p.PaidOn >= start &&
                        p.PaidOn < end)
                    .Sum(p => (decimal?)p.Amount) ?? 0m;

                return new
                {
                    Month = start.ToString("MMM yyyy"),
                    Revenue = revenue
                };
            })
            .ToList();

        var totalUsers = _context.Members.Count();
        var membershipEndingSoonCount = _context.MemberMemberships
            .Count(mm => mm.EndDate >= today && mm.EndDate <= endingSoonTill);

        var activeMembershipUsers = _context.MemberMemberships
            .AsNoTracking()
            .Where(mm => mm.StartDate <= today && mm.EndDate >= today)
            .Select(mm => new { mm.MemberId, mm.SecondaryMemberId })
            .ToList();

        var activeUserIds = activeMembershipUsers
            .Select(x => x.MemberId)
            .Concat(activeMembershipUsers
                .Where(x => x.SecondaryMemberId.HasValue)
                .Select(x => x.SecondaryMemberId!.Value))
            .Distinct()
            .ToHashSet();

        var endedMembershipUsers = _context.MemberMemberships
            .AsNoTracking()
            .Where(mm => mm.EndDate < today)
            .Select(mm => new { mm.MemberId, mm.SecondaryMemberId })
            .ToList();

        var endedUserIds = endedMembershipUsers
            .Select(x => x.MemberId)
            .Concat(endedMembershipUsers
                .Where(x => x.SecondaryMemberId.HasValue)
                .Select(x => x.SecondaryMemberId!.Value))
            .Distinct()
            .ToHashSet();

        var activeUsers = activeUserIds.Count;
        var completedPlansCount = endedUserIds.Except(activeUserIds).Count();

        return Ok(new DashboardSummaryResponse
        {
            MonthlyRevenue = monthlyRevenue,
            PendingCollectionAmount = pendingCollectionAmount,
            RevenueTrend = trend
                .Select(x => new RevenueTrendPointResponse { Month = x.Month, Revenue = x.Revenue })
                .ToList(),
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            MembershipEndingSoonCount = membershipEndingSoonCount,
            CompletedPlansCount = completedPlansCount
        });
    }

    private void EnsureInvoiceSnapshotsForHistoricalPayments()
    {
        var paymentsNeedingSnapshot = _context.Payments
            .AsNoTracking()
            .Where(p => !_context.InvoiceSnapshots.Any(i => i.InvoiceNumber == p.InvoiceNumber))
            .OrderBy(p => p.PaidOn)
            .ToList();

        if (paymentsNeedingSnapshot.Count == 0)
        {
            return;
        }

        var gym = _context.GymProfiles.FirstOrDefault() ?? BuildFallbackGymProfile();

        foreach (var payment in paymentsNeedingSnapshot)
        {
            var member = _context.Members.FirstOrDefault(m => m.Id == payment.MemberId);
            if (member is null)
            {
                continue;
            }

            var planName = GetPlanName(payment.MemberMembershipId);
            decimal billedAmount = payment.Amount;
            if (payment.MemberMembershipId.HasValue)
            {
                var financials = GetMemberMembershipFinancials(payment.MemberMembershipId.Value);
                billedAmount = financials.NetPlanAmount;
            }

            EnsureInvoiceSnapshot(payment, member, gym, planName, billedAmount);
        }
    }

    private static string GenerateInvoiceNumber()
    {
        return $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
    }

    private static string GenerateReceiptNumber()
    {
        return $"REC-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
    }

    private string? GetPlanName(Guid? memberMembershipId)
    {
        if (!memberMembershipId.HasValue)
        {
            return null;
        }

        return (from mm in _context.MemberMemberships
                join mp in _context.MembershipPlans on mm.MembershipPlanId equals mp.Id
                where mm.Id == memberMembershipId.Value
                select mp.PlanName).FirstOrDefault();
    }

    private InvoicePdfData EnsureInvoiceSnapshot(Payment payment, Member member, GymProfile gym, string? planName)
    {
        decimal billedAmount = payment.Amount;
        if (payment.MemberMembershipId.HasValue)
        {
            var financials = GetMemberMembershipFinancials(payment.MemberMembershipId.Value);
            billedAmount = financials.NetPlanAmount;
        }

        return EnsureInvoiceSnapshot(payment, member, gym, planName, billedAmount);
    }

    private InvoicePdfData EnsureInvoiceSnapshot(Payment payment, Member member, GymProfile gym, string? planName, decimal billedAmount)
    {
        var existing = _context.InvoiceSnapshots.FirstOrDefault(x => x.InvoiceNumber == payment.InvoiceNumber);
        if (existing is not null && !string.IsNullOrWhiteSpace(existing.SnapshotJson))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<InvoicePdfData>(existing.SnapshotJson);
                if (snapshot is not null)
                {
                    snapshot.LogoBytes ??= TryGetImageBytes(snapshot.Gym.LogoDataUri);
                    return snapshot;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize invoice snapshot {InvoiceNumber}. Rebuilding snapshot.", payment.InvoiceNumber);
            }
        }

        var invoiceData = BuildInvoiceData(payment, member, gym, planName, billedAmount);
        var existingLogoBytes = invoiceData.LogoBytes;
        invoiceData.LogoBytes = null;
        var json = JsonSerializer.Serialize(invoiceData);
        invoiceData.LogoBytes = existingLogoBytes;

        if (existing is null)
        {
            _context.InvoiceSnapshots.Add(new InvoiceSnapshot
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = payment.InvoiceNumber,
                MemberId = payment.MemberId,
                MemberMembershipId = payment.MemberMembershipId,
                InvoiceDate = invoiceData.InvoiceDate,
                SnapshotJson = json,
                CreatedOn = DateTime.UtcNow
            });
        }
        else
        {
            existing.InvoiceDate = invoiceData.InvoiceDate;
            existing.SnapshotJson = json;
        }

        _context.SaveChanges();
        return invoiceData;
    }

    private InvoicePdfData BuildInvoiceData(Payment payment, Member member, GymProfile gym, string? planName, decimal billedAmount)
    {
        var gstRatePercent = Math.Clamp(gym.GstRatePercent, 0m, 100m);
        var gstRate = gstRatePercent / 100m;
        var isGstApplicable = gym.IsGstApplicable;
        var hsnSac = string.IsNullOrWhiteSpace(gym.HsnSacCode) ? "9997" : gym.HsnSacCode.Trim();

        decimal baseAmountBeforeDiscount = billedAmount;
        decimal discountAmount = 0m;
        decimal discountPercent = 0m;
        string planDescription = payment.Notes ?? string.Empty;
        string? planStartDate = null;
        string? planEndDate = null;
        var invoiceDate = payment.PaidOn;
        List<ReceiptLine> receipts;

        if (payment.MemberMembershipId.HasValue)
        {
            var membership = _context.MemberMemberships.FirstOrDefault(mm => mm.Id == payment.MemberMembershipId.Value);
            var plan = membership is null ? null : _context.MembershipPlans.FirstOrDefault(mp => mp.Id == membership.MembershipPlanId);

            if (membership is not null && plan is not null)
            {
                planName = plan.PlanName;
                planDescription = string.IsNullOrWhiteSpace(membership.Description) ? plan.Description : membership.Description;
                planStartDate = membership.StartDate.ToString("dd-MMM-yyyy");
                planEndDate = membership.EndDate.ToString("dd-MMM-yyyy");
                baseAmountBeforeDiscount = membership.PlanPriceAtEnrollment > 0m ? membership.PlanPriceAtEnrollment : plan.Price;
                discountAmount = Math.Min(Math.Max(membership.Discount, 0m), baseAmountBeforeDiscount);
                discountPercent = baseAmountBeforeDiscount <= 0m
                    ? 0m
                    : Math.Round((discountAmount / baseAmountBeforeDiscount) * 100m, 2);
            }

            receipts = _context.Payments
                .Where(p => p.MemberMembershipId == payment.MemberMembershipId.Value)
                .OrderBy(p => p.PaidOn)
                .Select(p => new ReceiptLine
                {
                    ReceiptNumber = p.ReceiptNumber,
                    PaidOn = p.PaidOn,
                    Amount = p.Amount,
                    PaymentMode = p.PaymentMode.ToString()
                })
                .ToList();

            if (receipts.Count > 0)
            {
                invoiceDate = receipts.Max(x => x.PaidOn);
            }
        }
        else
        {
            receipts = new List<ReceiptLine>
            {
                new()
                {
                    ReceiptNumber = payment.ReceiptNumber,
                    PaidOn = payment.PaidOn,
                    Amount = payment.Amount,
                    PaymentMode = payment.PaymentMode.ToString()
                }
            };
        }

        var totalInvoiceAmount = RoundMoney(Math.Max(baseAmountBeforeDiscount - discountAmount, 0m));
        decimal taxableAmount;
        decimal gstAmount;
        decimal cgstAmount;
        decimal sgstAmount;

        if (isGstApplicable && gstRate > 0m)
        {
            taxableAmount = RoundMoney(totalInvoiceAmount / (1 + gstRate));
            gstAmount = RoundMoney(totalInvoiceAmount - taxableAmount);
            cgstAmount = RoundMoney(gstAmount / 2m);
            sgstAmount = RoundMoney(gstAmount - cgstAmount);
        }
        else
        {
            taxableAmount = totalInvoiceAmount;
            gstAmount = 0m;
            cgstAmount = 0m;
            sgstAmount = 0m;
        }

        var totalCollected = RoundMoney(receipts.Sum(r => r.Amount));
        var totalDue = RoundMoney(Math.Max(totalInvoiceAmount - totalCollected, 0m));

        return new InvoicePdfData
        {
            InvoiceNumber = payment.InvoiceNumber,
            InvoiceDate = invoiceDate,
            MemberName = member.Name,
            MemberEmail = member.Email,
            MemberPhone = member.Phone,
            MemberAddressLine = member.AddressLine,
            MemberCity = member.City,
            MemberState = member.State,
            MemberPincode = member.Pincode,
            PlanName = planName ?? "N/A",
            PlanDescription = planDescription,
            PlanStartDate = planStartDate,
            PlanEndDate = planEndDate,
            Gym = gym,
            HsnSacCode = hsnSac,
            IsGstApplicable = isGstApplicable,
            GstRatePercent = isGstApplicable ? gstRatePercent : 0m,
            CgstRatePercent = isGstApplicable ? RoundMoney(gstRatePercent / 2m) : 0m,
            SgstRatePercent = isGstApplicable ? RoundMoney(gstRatePercent / 2m) : 0m,
            BaseAmountBeforeDiscount = RoundMoney(baseAmountBeforeDiscount),
            DiscountAmount = RoundMoney(discountAmount),
            DiscountPercent = discountPercent,
            TaxableAmount = taxableAmount,
            GstAmount = gstAmount,
            CgstAmount = cgstAmount,
            SgstAmount = sgstAmount,
            TotalInvoiceAmount = totalInvoiceAmount,
            TotalCollected = totalCollected,
            TotalDue = totalDue,
            AmountInWords = AmountToWordsInr(totalInvoiceAmount),
            LogoBytes = TryGetImageBytes(gym.LogoDataUri),
            Receipts = receipts
        };
    }

    private static byte[] BuildInvoicePdf(InvoicePdfData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(78).AlignMiddle().AlignLeft().PaddingRight(8).Element(c =>
                        {
                            if (data.LogoBytes is { Length: > 0 })
                            {
                                c.Height(64).Width(64).Image(data.LogoBytes);
                            }
                            else
                            {
                                c.Height(64).Width(64).Border(1).BorderColor("#CBD5E1")
                                    .AlignCenter().AlignMiddle().Text("LOGO").FontColor("#94A3B8").FontSize(9);
                            }
                        });

                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text(data.Gym.GymName).Bold().FontSize(24).FontColor("#0E7490");
                            left.Item().Text("BILLING MADE EASY").Bold().FontColor("#475569").FontSize(12);
                            left.Item().Text($"{data.Gym.AddressLine}, {data.Gym.City}, {data.Gym.State} - {data.Gym.Pincode}");
                            left.Item().Text($"Email: {data.Gym.Email} | Phone: {data.Gym.Phone}");
                            if (data.IsGstApplicable && !string.IsNullOrWhiteSpace(data.Gym.GstNumber))
                            {
                                left.Item().Text($"GSTIN: {data.Gym.GstNumber}");
                            }
                        });
                        row.ConstantItem(250).AlignRight().Column(right =>
                        {
                            right.Item().Text("ORIGINAL FOR RECIPIENT").FontSize(11).SemiBold().FontColor("#334155");
                            right.Item().Text("INVOICE").Bold().FontSize(30).FontColor("#0284C7");
                            right.Item().Text($"Invoice No: {data.InvoiceNumber}").Bold();
                            right.Item().Text($"Invoice Date: {data.InvoiceDate:dd-MMM-yyyy}");
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(2).LineColor("#0EA5E9");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Bill To").Bold().FontSize(14).FontColor("#0284C7");
                            left.Item().Text(data.MemberName).Bold();
                            left.Item().Text($"{data.MemberAddressLine}");
                            left.Item().Text($"{data.MemberCity}, {data.MemberState} - {data.MemberPincode}");
                            left.Item().Text($"Phone: {data.MemberPhone}");
                            left.Item().Text($"Email: {data.MemberEmail}");
                        });
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("Plan Details").Bold().FontSize(14).FontColor("#0284C7");
                            right.Item().Text(data.PlanName).Bold();
                            right.Item().Text(string.IsNullOrWhiteSpace(data.PlanDescription) ? "N/A" : data.PlanDescription);
                            right.Item().Text($"Amount (Inclusive): INR {data.BaseAmountBeforeDiscount:N2}");
                            if (!string.IsNullOrWhiteSpace(data.PlanStartDate) && !string.IsNullOrWhiteSpace(data.PlanEndDate))
                            {
                                right.Item().Text($"Plan Start: {data.PlanStartDate}");
                                right.Item().Text($"Plan End: {data.PlanEndDate}");
                            }
                        });
                    });

                    col.Item().PaddingTop(4).Table(table =>
                    {
                        var hasDiscount = data.DiscountAmount > 0m;
                        var showGstColumn = data.IsGstApplicable;

                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn(3.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2);
                            if (hasDiscount)
                            {
                                columns.RelativeColumn(2);
                            }
                            if (showGstColumn)
                            {
                                columns.RelativeColumn(2);
                            }
                            columns.RelativeColumn(2);
                        });

                        static IContainer HeaderCell(IContainer c) => c.Padding(5).Background("#E0F2FE").BorderBottom(1).BorderColor("#7DD3FC");
                        static IContainer BodyCell(IContainer c) => c.Padding(5).BorderBottom(1).BorderColor("#E5E7EB");

                        table.Cell().Element(HeaderCell).Text("No").Bold();
                        table.Cell().Element(HeaderCell).Text("Product / Service").Bold();
                        table.Cell().Element(HeaderCell).Text("HSN/SAC").Bold();
                        table.Cell().Element(HeaderCell).Text("Amount").Bold();
                        if (hasDiscount)
                        {
                            table.Cell().Element(HeaderCell).Text("Discount").Bold();
                        }
                        if (showGstColumn)
                        {
                            table.Cell().Element(HeaderCell).Text("GST (Included)").Bold();
                        }
                        table.Cell().Element(HeaderCell).Text("Line Total").Bold();

                        table.Cell().Element(BodyCell).Text("1");
                        table.Cell().Element(BodyCell).Column(x =>
                        {
                            x.Item().Text(data.PlanName).Bold();
                            x.Item().Text(string.IsNullOrWhiteSpace(data.PlanDescription) ? "N/A" : data.PlanDescription);
                        });
                        table.Cell().Element(BodyCell).Text(data.HsnSacCode);
                        table.Cell().Element(BodyCell).AlignRight().Text($"INR {data.BaseAmountBeforeDiscount:N2}");
                        if (hasDiscount)
                        {
                            table.Cell().Element(BodyCell).AlignRight().Column(x =>
                            {
                                x.Item().Text($"INR {data.DiscountAmount:N2}");
                                x.Item().Text(data.DiscountPercent > 0 ? $"({data.DiscountPercent:N2}%)" : "(0%)").FontSize(9);
                            });
                        }
                        if (showGstColumn)
                        {
                            table.Cell().Element(BodyCell).AlignRight().Text($"INR {data.GstAmount:N2}");
                        }
                        table.Cell().Element(BodyCell).AlignRight().Text($"INR {data.TotalInvoiceAmount:N2}").Bold();
                    });

                    col.Item().Text("Collected Payments").Bold().FontColor("#0284C7");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        static IContainer HeaderCell(IContainer c) => c.Padding(5).Background("#F1F5F9").BorderBottom(1).BorderColor("#CBD5E1");
                        static IContainer BodyCell(IContainer c) => c.Padding(5).BorderBottom(1).BorderColor("#E5E7EB");

                        table.Cell().Element(HeaderCell).Text("Receipt No").Bold();
                        table.Cell().Element(HeaderCell).Text("Date").Bold();
                        table.Cell().Element(HeaderCell).Text("Mode").Bold();
                        table.Cell().Element(HeaderCell).AlignRight().Text("Amount").Bold();

                        foreach (var receipt in data.Receipts)
                        {
                            table.Cell().Element(BodyCell).Text(receipt.ReceiptNumber);
                            table.Cell().Element(BodyCell).Text(receipt.PaidOn.ToString("dd-MMM-yyyy"));
                            table.Cell().Element(BodyCell).Text(receipt.PaymentMode);
                            table.Cell().Element(BodyCell).AlignRight().Text($"INR {receipt.Amount:N2}");
                        }
                    });

                    col.Item().AlignRight().Width(260).Column(x =>
                    {
                        x.Spacing(2);
                        x.Item().Row(r => { r.RelativeItem().Text("Total Before Discount"); r.ConstantItem(110).AlignRight().Text($"INR {data.BaseAmountBeforeDiscount:N2}"); });
                        if (data.DiscountAmount > 0m)
                        {
                            x.Item().Row(r => { r.RelativeItem().Text($"Discount ({data.DiscountPercent:N2}%)"); r.ConstantItem(110).AlignRight().Text($"- INR {data.DiscountAmount:N2}"); });
                        }
                        if (data.IsGstApplicable)
                        {
                            x.Item().Row(r => { r.RelativeItem().Text("Taxable Value"); r.ConstantItem(110).AlignRight().Text($"INR {data.TaxableAmount:N2}"); });
                            x.Item().Row(r => { r.RelativeItem().Text($"CGST ({data.CgstRatePercent:N2}%)"); r.ConstantItem(110).AlignRight().Text($"INR {data.CgstAmount:N2}"); });
                            x.Item().Row(r => { r.RelativeItem().Text($"SGST ({data.SgstRatePercent:N2}%)"); r.ConstantItem(110).AlignRight().Text($"INR {data.SgstAmount:N2}"); });
                        }
                        x.Item().Row(r => { r.RelativeItem().Text("Total Invoice").Bold(); r.ConstantItem(110).AlignRight().Text($"INR {data.TotalInvoiceAmount:N2}").Bold(); });
                        x.Item().Row(r => { r.RelativeItem().Text("Collected"); r.ConstantItem(110).AlignRight().Text($"INR {data.TotalCollected:N2}"); });
                        x.Item().Row(r => { r.RelativeItem().Text("Amount Due").Bold(); r.ConstantItem(110).AlignRight().Text($"INR {data.TotalDue:N2}").Bold(); });
                    });

                    col.Item().PaddingTop(4).Text($"Amount in words: INR {data.AmountInWords}")
                        .SemiBold().FontColor("#0F172A");

                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#0EA5E9");
                    col.Item().PaddingTop(6).Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(8).Column(bank =>
                    {
                        bank.Spacing(4);
                        bank.Item().Text("Bank Details").Bold().FontColor("#0284C7");
                        bank.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            static IContainer Cell(IContainer c) => c.PaddingVertical(1);

                            table.Cell().Element(Cell).Text($"Bank: {data.Gym.BankName} | IFSC: {data.Gym.IfscCode}");
                            table.Cell().Element(Cell).Text($"UPI: {(string.IsNullOrWhiteSpace(data.Gym.UpiId) ? "N/A" : data.Gym.UpiId)}");
                            table.Cell().Element(Cell).Text($"A/C Name: {data.Gym.AccountName}");
                            table.Cell().Element(Cell).Text($"A/C No: {data.Gym.AccountNumber}");
                        });
                    });
                    col.Item().PaddingTop(6).Text(data.IsGstApplicable
                        ? $"Pricing is inclusive of {data.GstRatePercent:N2}% GST."
                        : "GST is not applicable for this invoice.");

                    col.Item().PaddingTop(10).AlignRight().Width(230).Border(1).BorderColor("#CBD5E1").Padding(8).Column(sign =>
                    {
                        sign.Item().AlignCenter().Text($"For {data.Gym.GymName}").SemiBold().FontColor("#334155");
                        sign.Item().Height(34);
                        sign.Item().LineHorizontal(1).LineColor("#94A3B8");
                        sign.Item().AlignCenter().PaddingTop(4).Text("Authorized Signatory").FontSize(9).FontColor("#475569");
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Thank you for your business.");
                    x.Span($" Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            });
        }).GeneratePdf();
    }

    private ReceiptPdfData BuildReceiptData(Payment payment, Member member, GymProfile gym, string? planName)
    {
        decimal? planTotalAmount = null;
        decimal? totalPaidForPlan = null;
        decimal? dueForPlan = null;
        string? planStartDate = null;
        string? planEndDate = null;

        if (payment.MemberMembershipId.HasValue)
        {
            var financials = GetMemberMembershipFinancials(payment.MemberMembershipId.Value);
            planTotalAmount = financials.NetPlanAmount;
            totalPaidForPlan = financials.CollectedAmount;
            dueForPlan = financials.DueAmount;

            var membership = _context.MemberMemberships.FirstOrDefault(mm => mm.Id == payment.MemberMembershipId.Value);
            if (membership is not null)
            {
                planStartDate = membership.StartDate.ToString("dd-MMM-yyyy");
                planEndDate = membership.EndDate.ToString("dd-MMM-yyyy");
            }
        }

        return new ReceiptPdfData
        {
            ReceiptNumber = payment.ReceiptNumber,
            LinkedInvoiceNumber = payment.InvoiceNumber,
            ReceiptDate = payment.PaidOn,
            GymName = gym.GymName,
            GymAddress = $"{gym.AddressLine}, {gym.City}, {gym.State} - {gym.Pincode}",
            GymPhone = gym.Phone,
            GymEmail = gym.Email,
            LogoBytes = TryGetImageBytes(gym.LogoDataUri),
            MemberName = member.Name,
            MemberAddress = $"{member.AddressLine}, {member.City}, {member.State} - {member.Pincode}",
            MemberPhone = member.Phone,
            MemberEmail = member.Email,
            PlanName = planName ?? "N/A",
            PlanStartDate = planStartDate,
            PlanEndDate = planEndDate,
            AmountReceived = payment.Amount,
            AmountInWords = AmountToWordsInr(payment.Amount),
            PaymentMode = payment.PaymentMode.ToString(),
            TransactionReference = payment.TransactionReference,
            Notes = payment.Notes,
            PlanTotalAmount = planTotalAmount,
            TotalPaidForPlan = totalPaidForPlan,
            DueForPlan = dueForPlan
        };
    }

    private static byte[] BuildReceiptPdf(ReceiptPdfData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Height(10).Background("#0E7490");
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(78).AlignMiddle().AlignLeft().Padding(6).Element(c =>
                        {
                            if (data.LogoBytes is { Length: > 0 })
                            {
                                c.Height(58).Width(58).Image(data.LogoBytes);
                            }
                            else
                            {
                                c.Height(58).Width(58).Border(1).BorderColor("#CBD5E1")
                                    .AlignCenter().AlignMiddle().Text("LOGO").FontColor("#94A3B8").FontSize(9);
                            }
                        });
                        row.RelativeItem().PaddingTop(6).Column(center =>
                        {
                            center.Item().Text("PAYMENT RECEIPT").Bold().FontSize(30).FontColor("#0284C7");
                            center.Item().Text(data.GymName).SemiBold().FontSize(13).FontColor("#0F172A");
                            center.Item().Text($"{data.GymPhone} | {data.GymEmail}").FontColor("#334155");
                            center.Item().Text("BILLING MADE EASY").Bold().FontSize(11).FontColor("#475569");
                        });
                        row.ConstantItem(250).PaddingTop(8).Column(right =>
                        {
                            right.Item().Text(data.GymAddress);
                            right.Item().Text($"Receipt No: {data.ReceiptNumber}").Bold();
                            right.Item().Text($"Date: {data.ReceiptDate:dd-MMM-yyyy}");
                        });
                    });
                    col.Item().PaddingTop(3).Height(7).Background("#F59E0B");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    col.Item().PaddingTop(8).Text($"Received with thanks from: {data.MemberName}").Bold().FontSize(13);
                    col.Item().Text($"Address: {data.MemberAddress}");
                    col.Item().Text($"Contact: {data.MemberPhone} | {data.MemberEmail}");

                    col.Item().PaddingTop(2).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.2f);
                            columns.RelativeColumn(2.2f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(2f);
                        });

                        static IContainer Head(IContainer c) => c.Padding(6).Background("#E0F2FE").BorderBottom(1).BorderColor("#7DD3FC");
                        static IContainer Body(IContainer c) => c.Padding(6).BorderBottom(1).BorderColor("#E5E7EB");

                        table.Cell().Element(Head).Text("For (Plan)").Bold();
                        table.Cell().Element(Head).Text("Reference").Bold();
                        table.Cell().Element(Head).Text("Mode").Bold();
                        table.Cell().Element(Head).AlignRight().Text("Amount").Bold();

                        table.Cell().Element(Body).Column(x =>
                        {
                            x.Item().Text(data.PlanName);
                            if (!string.IsNullOrWhiteSpace(data.PlanStartDate) && !string.IsNullOrWhiteSpace(data.PlanEndDate))
                            {
                                x.Item().Text($"Period: {data.PlanStartDate} to {data.PlanEndDate}").FontSize(9).FontColor("#475569");
                            }
                        });
                        table.Cell().Element(Body).Column(x =>
                        {
                            x.Item().Text($"Invoice: {data.LinkedInvoiceNumber}");
                            x.Item().Text($"Receipt: {data.ReceiptNumber}");
                            x.Item().Text($"Txn Ref: {data.TransactionReference ?? "N/A"}").FontSize(9);
                        });
                        table.Cell().Element(Body).Text(data.PaymentMode);
                        table.Cell().Element(Body).AlignRight().Text($"INR {data.AmountReceived:N2}").Bold();
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Amount in words: INR {data.AmountInWords}").SemiBold();
                    });

                    if (data.PlanTotalAmount.HasValue)
                    {
                        col.Item().AlignRight().Width(290).Column(x =>
                        {
                            x.Spacing(2);
                            x.Item().Row(r => { r.RelativeItem().Text("Plan Total"); r.ConstantItem(120).AlignRight().Text($"INR {data.PlanTotalAmount.Value:N2}"); });
                            x.Item().Row(r => { r.RelativeItem().Text("Paid Till Date"); r.ConstantItem(120).AlignRight().Text($"INR {data.TotalPaidForPlan.GetValueOrDefault():N2}"); });
                            x.Item().Row(r => { r.RelativeItem().Text("Due Amount").Bold(); r.ConstantItem(120).AlignRight().Text($"INR {data.DueForPlan.GetValueOrDefault():N2}").Bold(); });
                        });
                    }

                    col.Item().PaddingTop(4).Text($"Notes: {data.Notes ?? "N/A"}");

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#94A3B8");
                            c.Item().AlignCenter().Text("Received By").FontSize(9).FontColor("#475569");
                        });
                        row.ConstantItem(24);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#94A3B8");
                            c.Item().AlignCenter().Text("Authorized Signature").FontSize(9).FontColor("#475569");
                        });
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().Height(7).Background("#F59E0B");
                    col.Item().PaddingTop(4).AlignCenter().Text("This is a system-generated payment receipt.");
                });
            });
        }).GeneratePdf();
    }

    private static GymProfile BuildFallbackGymProfile()
    {
        return new GymProfile
        {
            Id = Guid.Empty,
            GymName = "Gym Management",
            Email = "N/A",
            Phone = "N/A",
            AddressLine = "N/A",
            City = "N/A",
            State = "N/A",
            Pincode = "N/A",
            GstNumber = "N/A",
            BankName = "N/A",
            AccountName = "N/A",
            AccountNumber = "N/A",
            IfscCode = "N/A",
            UpiId = "N/A",
            HsnSacCode = "9997",
            GstRatePercent = 18m,
            IsGstApplicable = false
        };
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static byte[]? TryGetImageBytes(string? logoDataUri)
    {
        if (string.IsNullOrWhiteSpace(logoDataUri))
        {
            return null;
        }

        var commaIndex = logoDataUri.IndexOf(',');
        var base64 = commaIndex >= 0 ? logoDataUri[(commaIndex + 1)..] : logoDataUri;

        try
        {
            return Convert.FromBase64String(base64);
        }
        catch
        {
            return null;
        }
    }

    private static string AmountToWordsInr(decimal amount)
    {
        var whole = (long)Math.Floor(amount);
        var paise = (int)Math.Round((amount - whole) * 100m, MidpointRounding.AwayFromZero);

        var words = $"{NumberToWordsIndian(whole)} Rupees";
        if (paise > 0)
        {
            words += $" and {NumberToWordsIndian(paise)} Paise";
        }

        return $"{words} Only";
    }

    private static string NumberToWordsIndian(long number)
    {
        if (number == 0) return "Zero";
        if (number < 0) return $"Minus {NumberToWordsIndian(Math.Abs(number))}";

        var ones = new[]
        {
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen",
            "Eighteen", "Nineteen"
        };
        var tens = new[] { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

        string TwoDigits(long n) => n < 20 ? ones[n] : $"{tens[n / 10]}{(n % 10 > 0 ? " " + ones[n % 10] : "")}";
        string ThreeDigits(long n)
        {
            if (n < 100) return TwoDigits(n);
            var rem = n % 100;
            return $"{ones[n / 100]} Hundred{(rem > 0 ? " " + TwoDigits(rem) : "")}";
        }

        var crore = number / 10000000;
        number %= 10000000;
        var lakh = number / 100000;
        number %= 100000;
        var thousand = number / 1000;
        number %= 1000;
        var hundred = number;

        var parts = new List<string>();
        if (crore > 0) parts.Add($"{TwoDigits(crore)} Crore");
        if (lakh > 0) parts.Add($"{TwoDigits(lakh)} Lakh");
        if (thousand > 0) parts.Add($"{TwoDigits(thousand)} Thousand");
        if (hundred > 0) parts.Add(ThreeDigits(hundred));

        return string.Join(" ", parts);
    }

    private bool CanAccessMemberData(Guid memberId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return false;
        }

        var member = _context.Members.FirstOrDefault(m => m.Id == memberId);
        return member is not null && member.UserId == currentUserId;
    }

    private decimal GetDueForMemberMembership(Guid memberMembershipId)
    {
        var membership = _context.MemberMemberships.FirstOrDefault(mm => mm.Id == memberMembershipId);
        if (membership is null)
        {
            return 0m;
        }

        var plan = _context.MembershipPlans.FirstOrDefault(mp => mp.Id == membership.MembershipPlanId);
        if (plan is null)
        {
            return 0m;
        }

        var basePlanAmount = membership.PlanPriceAtEnrollment > 0m ? membership.PlanPriceAtEnrollment : plan.Price;
        var planAmount = Math.Max(basePlanAmount - membership.Discount, 0m);
        var collected = _context.Payments
            .Where(p => p.MemberMembershipId == memberMembershipId)
            .Sum(p => (decimal?)p.Amount) ?? 0m;

        return Math.Max(planAmount - collected, 0m);
    }

    private (decimal NetPlanAmount, decimal CollectedAmount, decimal DueAmount, string? PlanName) GetMemberMembershipFinancials(Guid memberMembershipId)
    {
        var membership = _context.MemberMemberships.FirstOrDefault(mm => mm.Id == memberMembershipId);
        if (membership is null)
        {
            return (0m, 0m, 0m, null);
        }

        var plan = _context.MembershipPlans.FirstOrDefault(mp => mp.Id == membership.MembershipPlanId);
        if (plan is null)
        {
            return (0m, 0m, 0m, null);
        }

        var basePlanAmount = membership.PlanPriceAtEnrollment > 0m ? membership.PlanPriceAtEnrollment : plan.Price;
        var netPlanAmount = Math.Max(basePlanAmount - membership.Discount, 0m);
        var collected = _context.Payments
            .Where(p => p.MemberMembershipId == memberMembershipId)
            .Sum(p => (decimal?)p.Amount) ?? 0m;
        var due = Math.Max(netPlanAmount - collected, 0m);
        return (netPlanAmount, collected, due, plan.PlanName);
    }

    private static PaymentResponse ToPaymentResponse(Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            MemberId = payment.MemberId,
            MemberMembershipId = payment.MemberMembershipId,
            ReceiptNumber = payment.ReceiptNumber,
            Amount = payment.Amount,
            PaidOn = payment.PaidOn,
            PaymentMode = payment.PaymentMode,
            TransactionReference = payment.TransactionReference,
            Notes = payment.Notes,
            InvoiceNumber = payment.InvoiceNumber
        };
    }

    public class InvoicePdfData
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string MemberPhone { get; set; } = string.Empty;
        public string MemberAddressLine { get; set; } = string.Empty;
        public string MemberCity { get; set; } = string.Empty;
        public string MemberState { get; set; } = string.Empty;
        public string MemberPincode { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string PlanDescription { get; set; } = string.Empty;
        public string? PlanStartDate { get; set; }
        public string? PlanEndDate { get; set; }
        public string HsnSacCode { get; set; } = "9997";
        public string AmountInWords { get; set; } = string.Empty;
        public GymProfile Gym { get; set; } = new();
        public byte[]? LogoBytes { get; set; }
        public bool IsGstApplicable { get; set; }
        public decimal BaseAmountBeforeDiscount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal GstRatePercent { get; set; }
        public decimal CgstRatePercent { get; set; }
        public decimal SgstRatePercent { get; set; }
        public decimal GstAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal TotalInvoiceAmount { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalDue { get; set; }
        public List<ReceiptLine> Receipts { get; set; } = new();
    }

    public class ReceiptLine
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime PaidOn { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
    }

    public class ReceiptPdfData
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public string LinkedInvoiceNumber { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string GymPhone { get; set; } = string.Empty;
        public string GymEmail { get; set; } = string.Empty;
        public byte[]? LogoBytes { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberAddress { get; set; } = string.Empty;
        public string MemberPhone { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string? PlanStartDate { get; set; }
        public string? PlanEndDate { get; set; }
        public decimal AmountReceived { get; set; }
        public string AmountInWords { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }
        public decimal? PlanTotalAmount { get; set; }
        public decimal? TotalPaidForPlan { get; set; }
        public decimal? DueForPlan { get; set; }
    }
}
