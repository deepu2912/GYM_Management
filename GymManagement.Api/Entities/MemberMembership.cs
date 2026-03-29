namespace GymManagement.Api.Entities;

public class MemberMembership
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid? SecondaryMemberId { get; set; }
    public Guid MembershipPlanId { get; set; }
    public string? MasterInvoiceNumber { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public decimal PlanPriceAtEnrollment { get; set; }
    public decimal Discount { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public Member? Member { get; set; }
    public Member? SecondaryMember { get; set; }
    public MembershipPlan? MembershipPlan { get; set; }
}
