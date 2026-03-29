namespace GymManagement.Api.Entities;

public class Member
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public int Age => CalculateAge(DateOfBirth, DateOnly.FromDateTime(DateTime.UtcNow));
    public Gender Gender { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public DateTime JoiningDate { get; set; }
    public MembershipStatus MembershipStatus { get; set; } = MembershipStatus.Active;
    public Guid? GymTenantId { get; set; }
    public Guid? AssignedTrainerId { get; set; }
    public string? ProfilePhotoPath { get; set; }
    public List<MemberMembership> Memberships { get; set; } = new();

    private static int CalculateAge(DateOnly dob, DateOnly today)
    {
        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
