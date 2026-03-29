using System.ComponentModel.DataAnnotations;
using GymManagement.Api.Entities;

namespace GymManagement.Api.DTOs;

public class RecordPaymentRequest
{
    [Required]
    public Guid MemberId { get; set; }

    public Guid? MemberMembershipId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public DateTime PaidOn { get; set; } = DateTime.UtcNow;

    [Required]
    public PaymentMode PaymentMode { get; set; }

    public string? TransactionReference { get; set; }
    public string? Notes { get; set; }
}

public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid? MemberMembershipId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidOn { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public string? TransactionReference { get; set; }
    public string? Notes { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
}

public class MemberMembershipPaymentSummaryResponse
{
    public Guid MemberMembershipId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal PlanAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal DueAmount { get; set; }
}

public class SendReminderRequest
{
    public string? Notes { get; set; }
}

public class RevenueTrendPointResponse
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class DashboardSummaryResponse
{
    public decimal MonthlyRevenue { get; set; }
    public decimal PendingCollectionAmount { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int MembershipEndingSoonCount { get; set; }
    public int CompletedPlansCount { get; set; }
    public List<RevenueTrendPointResponse> RevenueTrend { get; set; } = new();
}
