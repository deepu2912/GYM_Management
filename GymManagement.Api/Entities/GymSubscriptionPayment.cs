namespace GymManagement.Api.Entities;

public class GymSubscriptionPayment
{
    public Guid Id { get; set; }
    public Guid GymTenantId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidOn { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public string? Notes { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
