using System.ComponentModel.DataAnnotations;

namespace GymManagement.Api.DTOs;

public class CreateGymTenantRequest
{
    [Required]
    public string GymName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits.")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be exactly 6 digits.")]
    public string Pincode { get; set; } = string.Empty;

    [Required]
    public string GstNumber { get; set; } = string.Empty;

    [Required]
    public string BankName { get; set; } = string.Empty;

    [Required]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    public string IfscCode { get; set; } = string.Empty;

    public string? UpiId { get; set; }

    [Required]
    public string HsnSacCode { get; set; } = "9997";

    [Range(0, 100)]
    public decimal GstRatePercent { get; set; } = 18m;

    public bool IsGstApplicable { get; set; } = true;

    [Required]
    public string AdminName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;
}

public class UpdateGymTenantRequest
{
    [Required]
    public string GymName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits.")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be exactly 6 digits.")]
    public string Pincode { get; set; } = string.Empty;

    [Required]
    public string GstNumber { get; set; } = string.Empty;

    [Required]
    public string BankName { get; set; } = string.Empty;

    [Required]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    public string IfscCode { get; set; } = string.Empty;

    public string? UpiId { get; set; }

    [Required]
    public string HsnSacCode { get; set; } = "9997";

    [Range(0, 100)]
    public decimal GstRatePercent { get; set; } = 18m;

    public bool IsGstApplicable { get; set; } = true;
    public bool IsActive { get; set; } = true;

    [Required]
    public string AdminName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;

    [MinLength(6)]
    public string? AdminPassword { get; set; }
}

public class GymTenantResponse
{
    public Guid Id { get; set; }
    public string GymName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string GstNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string IfscCode { get; set; } = string.Empty;
    public string? UpiId { get; set; }
    public string HsnSacCode { get; set; } = string.Empty;
    public decimal GstRatePercent { get; set; }
    public bool IsGstApplicable { get; set; }
    public bool IsActive { get; set; }
    public string SubscriptionPlan { get; set; } = "None";
    public DateTime? SubscriptionValidTill { get; set; }
    public bool LifetimePlanActivated { get; set; }
    public DateTime CreatedOn { get; set; }
    public GymAdminResponse? Admin { get; set; }
}

public class GymAdminResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UpdateGymStatusRequest
{
    public bool IsActive { get; set; }
}

public class GymSubscriptionStatusResponse
{
    public string Plan { get; set; } = "None";
    public DateTime? ValidTill { get; set; }
    public bool LifetimePlanActivated { get; set; }
    public bool IsExpired { get; set; }
    public int DaysRemaining { get; set; }
    public decimal CurrentPlanAmount { get; set; }
}

public class ActivateGymSubscriptionRequest
{
    [Required]
    public string PlanCode { get; set; } = string.Empty;
}

public class CreateGymSubscriptionPaymentRequest
{
    [Required]
    public string PlanCode { get; set; } = string.Empty;

    [Required]
    public string PaymentMode { get; set; } = string.Empty;

    public string? TransactionReference { get; set; }
    public string? Notes { get; set; }
    public DateTime PaidOn { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
}

public class GymSubscriptionPaymentResponse
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidOn { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public DateTime? ValidTill { get; set; }
    public bool InvoiceEmailSent { get; set; }
    public string? InvoiceEmailError { get; set; }
}
