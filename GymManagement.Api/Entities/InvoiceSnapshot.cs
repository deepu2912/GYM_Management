namespace GymManagement.Api.Entities;

public class InvoiceSnapshot
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public Guid? MemberMembershipId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}

