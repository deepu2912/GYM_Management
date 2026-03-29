using System.ComponentModel.DataAnnotations;
using GymManagement.Api.Entities;

namespace GymManagement.Api.DTOs;

public class MarkAttendanceRequest
{
    [Required]
    public Guid MemberId { get; set; }

    public DateOnly AttendanceDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Required]
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    public TimeOnly? CheckInTime { get; set; }
    public string? Notes { get; set; }
}
