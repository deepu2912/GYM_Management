namespace GymManagement.Api.Entities;

public class Payment
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
