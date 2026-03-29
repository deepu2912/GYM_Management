namespace GymManagement.Api.Entities;

public class MembershipPlan
{
    public Guid Id { get; set; }
    public Guid? GymTenantId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public MembershipType MembershipType { get; set; } = MembershipType.Single;
    public MembershipDuration Duration { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<MemberMembership> MemberMemberships { get; set; } = new();
}
