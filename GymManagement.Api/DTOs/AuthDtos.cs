using System.ComponentModel.DataAnnotations;
using GymManagement.Api.Entities;

namespace GymManagement.Api.DTOs;

public class RegisterMemberRequest
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
    public DateOnly DateOfBirth { get; set; }

    [Required]
    public Gender Gender { get; set; }

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

    public decimal Height { get; set; }
    public decimal Weight { get; set; }
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? GymTenantId { get; set; }
    public bool MustChangePassword { get; set; }
    public string? ProfilePhotoDataUri { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UserProfileResponse
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePhotoDataUri { get; set; }
}

public class UpdateProfilePhotoRequest
{
    [Required]
    public string PhotoDataUri { get; set; } = string.Empty;
}
