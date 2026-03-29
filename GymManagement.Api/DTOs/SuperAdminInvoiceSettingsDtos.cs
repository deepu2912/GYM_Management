using System.ComponentModel.DataAnnotations;

namespace GymManagement.Api.DTOs;

public class UpsertSuperAdminInvoiceSettingsRequest
{
    [Required]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits.")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be exactly 6 digits.")]
    public string Pincode { get; set; } = string.Empty;

    [Required]
    public string GstNumber { get; set; } = string.Empty;

    [Required]
    public string BankName { get; set; } = string.Empty;

    [Required]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    public string IfscCode { get; set; } = string.Empty;

    public string? UpiId { get; set; }

    [Required]
    public string AuthorizedSignatory { get; set; } = string.Empty;

    [Required]
    public string TermsAndConditions { get; set; } = string.Empty;
}

public class SuperAdminInvoiceSettingsResponse
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string GstNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string IfscCode { get; set; } = string.Empty;
    public string? UpiId { get; set; }
    public string AuthorizedSignatory { get; set; } = string.Empty;
    public string TermsAndConditions { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
}
