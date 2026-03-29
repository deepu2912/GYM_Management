using System.ComponentModel.DataAnnotations;

namespace GymManagement.Api.DTOs;

public class UpsertGymProfileRequest
{
    [Required]
    public string GymName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    public string Pincode { get; set; } = string.Empty;

    public string? GstNumber { get; set; }

    [Required]
    public string BankName { get; set; } = string.Empty;

    [Required]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    public string IfscCode { get; set; } = string.Empty;

    public string? UpiId { get; set; }
    public string? LogoDataUri { get; set; }
    [Required]
    public string HsnSacCode { get; set; } = "9997";
    [Range(0, 100)]
    public decimal GstRatePercent { get; set; } = 18m;
    public bool IsGstApplicable { get; set; } = true;
}
