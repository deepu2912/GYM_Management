using System.ComponentModel.DataAnnotations;
using GymManagement.Api.Entities;

namespace GymManagement.Api.DTOs;

public class CreateMemberRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [Required]
    public Gender Gender { get; set; }

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    public string Pincode { get; set; } = string.Empty;

    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public DateTime JoiningDate { get; set; } = DateTime.UtcNow.Date;
}

public class UpdateMemberRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [Required]
    public Gender Gender { get; set; }

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    public string Pincode { get; set; } = string.Empty;

    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public DateTime JoiningDate { get; set; }
    public MembershipStatus MembershipStatus { get; set; }
}

public class AssignTrainerRequest
{
    [Required]
    public Guid TrainerId { get; set; }
}

public class GetMembersPagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
}

public class MembersPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<Member> Items { get; set; } = new();
}
