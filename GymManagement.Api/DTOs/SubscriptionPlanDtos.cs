using System.ComponentModel.DataAnnotations;

namespace GymManagement.Api.DTOs;

public class SubscriptionPlanResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMonths { get; set; }
    public bool IsLifetime { get; set; }
    public bool IsMaintenance { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UpsertSubscriptionPlanRequest
{
    [Required]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Code can contain only letters and numbers.")]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(0, 1000000)]
    public decimal Price { get; set; }

    [Range(0, 120)]
    public int DurationMonths { get; set; }

    public bool IsLifetime { get; set; }
    public bool IsMaintenance { get; set; }
    public bool IsActive { get; set; } = true;

    [Range(0, 1000)]
    public int SortOrder { get; set; }

    public string Description { get; set; } = string.Empty;
}

public class UpdateSubscriptionPlanStatusRequest
{
    public bool IsActive { get; set; }
}
