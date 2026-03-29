using System.ComponentModel.DataAnnotations;
using GymManagement.Api.Entities;

namespace GymManagement.Api.DTOs;

public class CreateMembershipPlanRequest
{
    [Required]
    public string PlanName { get; set; } = string.Empty;

    [Required]
    public MembershipType MembershipType { get; set; } = MembershipType.Single;

    [Required]
    public MembershipDuration Duration { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;
}

public class MembershipPlanResponse
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public MembershipType MembershipType { get; set; } = MembershipType.Single;
    public MembershipDuration Duration { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool HasLinkedMemberships { get; set; }
}

public class UpdateMembershipPlanRequest
{
    [Required]
    public string PlanName { get; set; } = string.Empty;

    [Required]
    public MembershipType MembershipType { get; set; } = MembershipType.Single;

    [Required]
    public MembershipDuration Duration { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;
}

public class UpdateMembershipPlanStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}

public class CreateMemberMembershipRequest
{
    [Required]
    public Guid MemberId { get; set; }

    public Guid? SecondaryMemberId { get; set; }

    [Required]
    public Guid MembershipPlanId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; } = 0;

    public string Description { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }
}

public class UpdateMemberMembershipRequest
{
    [Required]
    public Guid MemberId { get; set; }

    public Guid? SecondaryMemberId { get; set; }

    [Required]
    public Guid MembershipPlanId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; } = 0;

    public string Description { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Required]
    public bool IsActive { get; set; }
}

public class MemberSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class MembershipPlanSummaryDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public MembershipType MembershipType { get; set; } = MembershipType.Single;
    public MembershipDuration Duration { get; set; }
    public decimal Price { get; set; }
}

public class MemberMembershipResponse
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid? SecondaryMemberId { get; set; }
    public Guid MembershipPlanId { get; set; }
    public DateTime CreatedOn { get; set; }
    public decimal PlanPriceAtEnrollment { get; set; }
    public decimal Discount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool HasPayments { get; set; }
    public MemberSummaryDto? Member { get; set; }
    public MemberSummaryDto? SecondaryMember { get; set; }
    public MembershipPlanSummaryDto? MembershipPlan { get; set; }
}

public class MemberMembershipDashboardSummaryResponse
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int CoupleMembershipCount { get; set; }
    public int CoupleUsersCount { get; set; }
    public int SingleUsersCount { get; set; }
    public int TotalMembershipUsers { get; set; }
}

public class GetMemberMembershipsPagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Name { get; set; }
    public Guid? PlanId { get; set; }
    public string? Type { get; set; }
    public DateOnly? CreatedFrom { get; set; }
    public DateOnly? CreatedTo { get; set; }
    public int? EndingInDays { get; set; }
    public bool PendingCollectionOnly { get; set; }
    public bool CollectedThisMonthOnly { get; set; }
}

public class MemberMembershipsPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<MemberMembershipResponse> Items { get; set; } = new();
}
