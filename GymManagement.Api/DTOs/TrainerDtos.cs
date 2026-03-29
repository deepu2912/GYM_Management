using System.ComponentModel.DataAnnotations;

namespace GymManagement.Api.DTOs;

public class CreateTrainerRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal BaseSalary { get; set; }

    public DateTime JoiningDate { get; set; } = DateTime.UtcNow.Date;
}

public class AssignMembersToTrainerRequest
{
    [Required]
    public List<Guid> MemberIds { get; set; } = new();
}

public class CreateTrainerSalaryRequest
{
    [Range(2000, 3000)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    public bool IsPaid { get; set; }
    public DateTime? PaidOn { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class UpdateTrainerSalaryRequest
{
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public bool IsPaid { get; set; }
    public DateTime? PaidOn { get; set; }
    public string Remarks { get; set; } = string.Empty;
}
