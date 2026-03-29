namespace GymManagement.Api.Entities;

public class GymTenant
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
    public string HsnSacCode { get; set; } = "9997";
    public decimal GstRatePercent { get; set; } = 18m;
    public bool IsGstApplicable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string SubscriptionPlan { get; set; } = "None";
    public DateTime? SubscriptionValidTill { get; set; }
    public bool LifetimePlanActivated { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
