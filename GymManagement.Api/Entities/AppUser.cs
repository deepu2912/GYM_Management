namespace GymManagement.Api.Entities;

public class AppUser
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? ProfilePhotoDataUri { get; set; }
    public bool MustChangePassword { get; set; }
    public AppRole Role { get; set; }
    public Guid? GymTenantId { get; set; }
    public GymTenant? GymTenant { get; set; }
}
