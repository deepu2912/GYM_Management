namespace GymManagement.Api.Entities;

public enum AppRole
{
    SuperAdmin,
    Admin,
    Trainer,
    Member
}

public enum Gender
{
    Male,
    Female,
    Other
}

public enum MembershipStatus
{
    Active,
    Inactive
}

public enum MembershipDuration
{
    OneMonth = 1,
    ThreeMonths = 3,
    SixMonths = 6,
    OneYear = 12
}

public enum MembershipType
{
    Single,
    Couple
}

public enum PaymentMode
{
    Upi,
    Cash,
    Card
}

public enum AttendanceStatus
{
    Present,
    Absent
}
