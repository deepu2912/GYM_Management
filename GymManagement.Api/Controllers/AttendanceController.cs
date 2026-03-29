using System.Security.Claims;
using GymManagement.Api.Data;
using GymManagement.Api.DTOs;
using GymManagement.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly AppDbContext _context;

    public AttendanceController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("mark")]
    [Authorize(Roles = "Admin,Trainer")]
    public IActionResult MarkAttendance([FromBody] MarkAttendanceRequest request)
    {
        var member = _context.Members.FirstOrDefault(m => m.Id == request.MemberId);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        var existing = _context.MemberAttendances.FirstOrDefault(a =>
            a.MemberId == request.MemberId && a.AttendanceDate == request.AttendanceDate);

        if (existing is null)
        {
            existing = new MemberAttendance
            {
                Id = Guid.NewGuid(),
                MemberId = request.MemberId,
                AttendanceDate = request.AttendanceDate,
                Status = request.Status,
                CheckInTime = request.CheckInTime,
                Notes = request.Notes?.Trim()
            };
            _context.MemberAttendances.Add(existing);
        }
        else
        {
            existing.Status = request.Status;
            existing.CheckInTime = request.CheckInTime;
            existing.Notes = request.Notes?.Trim();
        }

        _context.SaveChanges();
        return Ok(existing);
    }

    [HttpGet("report")]
    [Authorize(Roles = "Admin,Trainer,Member")]
    public IActionResult GetReport([FromQuery] Guid? memberId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        var query = _context.MemberAttendances.AsQueryable();

        if (User.IsInRole("Member"))
        {
            var currentMemberId = GetCurrentMemberId();
            if (currentMemberId is null)
            {
                return Forbid();
            }

            query = query.Where(a => a.MemberId == currentMemberId.Value);
        }
        else if (memberId.HasValue)
        {
            query = query.Where(a => a.MemberId == memberId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.AttendanceDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.AttendanceDate <= toDate.Value);
        }

        var report = query
            .OrderByDescending(a => a.AttendanceDate)
            .Select(a => new
            {
                a.Id,
                a.MemberId,
                MemberName = _context.Members.Where(m => m.Id == a.MemberId).Select(m => m.Name).FirstOrDefault(),
                a.AttendanceDate,
                a.Status,
                a.CheckInTime,
                a.Notes
            })
            .ToList();

        return Ok(report);
    }

    [HttpGet("summary/monthly")]
    [Authorize(Roles = "Admin,Trainer,Member")]
    public IActionResult GetMonthlySummary([FromQuery] int year, [FromQuery] int month, [FromQuery] Guid? memberId)
    {
        if (month is < 1 or > 12)
        {
            return BadRequest(new { message = "Month must be between 1 and 12." });
        }

        if (year is < 2000 or > 3000)
        {
            return BadRequest(new { message = "Year is out of range." });
        }

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var attendanceQuery = _context.MemberAttendances
            .Where(a => a.AttendanceDate >= startDate && a.AttendanceDate <= endDate);

        if (User.IsInRole("Member"))
        {
            var currentMemberId = GetCurrentMemberId();
            if (currentMemberId is null)
            {
                return Forbid();
            }

            attendanceQuery = attendanceQuery.Where(a => a.MemberId == currentMemberId.Value);
        }
        else if (memberId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.MemberId == memberId.Value);
        }

        var grouped = attendanceQuery
            .GroupBy(a => a.MemberId)
            .Select(g => new
            {
                MemberId = g.Key,
                TotalMarkedDays = g.Count(),
                PresentDays = g.Count(x => x.Status == AttendanceStatus.Present),
                AbsentDays = g.Count(x => x.Status == AttendanceStatus.Absent)
            })
            .ToList();

        var result = grouped
            .Select(g =>
            {
                var memberName = _context.Members.Where(m => m.Id == g.MemberId).Select(m => m.Name).FirstOrDefault();
                var percentage = g.TotalMarkedDays == 0 ? 0 : (decimal)g.PresentDays * 100 / g.TotalMarkedDays;
                return new
                {
                    g.MemberId,
                    MemberName = memberName,
                    g.TotalMarkedDays,
                    g.PresentDays,
                    g.AbsentDays,
                    AttendancePercentage = Math.Round(percentage, 2)
                };
            })
            .OrderByDescending(x => x.AttendancePercentage)
            .ToList();

        return Ok(new
        {
            Year = year,
            Month = month,
            Summary = result
        });
    }

    private Guid? GetCurrentMemberId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            return null;
        }

        return _context.Members.Where(m => m.UserId == currentUserId).Select(m => (Guid?)m.Id).FirstOrDefault();
    }
}
