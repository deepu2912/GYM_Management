namespace GymManagement.Api.Entities;

public class MemberAttendance
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public AttendanceStatus Status { get; set; }
    public TimeOnly? CheckInTime { get; set; }
    public string? Notes { get; set; }
}
