using System.Security.Claims;
using GymManagement.Api.Data;
using GymManagement.Api.DTOs;
using GymManagement.Api.Entities;
using GymManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrainersController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrainersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAll()
    {
        var trainers = (from user in _context.Users
                        join profile in _context.TrainerProfiles on user.Id equals profile.UserId
                        where user.Role == AppRole.Trainer
                        select new
                        {
                            user.Id,
                            user.Name,
                            user.Email,
                            profile.Phone,
                            profile.Specialization,
                            profile.BaseSalary,
                            profile.JoiningDate,
                            profile.IsActive,
                            AssignedMembersCount = _context.Members.Count(m => m.AssignedTrainerId == user.Id)
                        }).ToList();

        return Ok(trainers);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult Create([FromBody] CreateTrainerRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToUpper();
        var userExists = _context.Users.Any(u => u.Email.ToUpper() == normalizedEmail);
        if (userExists)
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = AppRole.Trainer
        };

        var profile = new TrainerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            Specialization = request.Specialization.Trim(),
            BaseSalary = request.BaseSalary,
            JoiningDate = request.JoiningDate,
            IsActive = true
        };

        _context.Users.Add(user);
        _context.TrainerProfiles.Add(profile);
        _context.SaveChanges();

        return CreatedAtAction(nameof(GetAll), new { id = user.Id }, new
        {
            user.Id,
            user.Name,
            user.Email,
            profile.Phone,
            profile.Specialization,
            profile.BaseSalary,
            profile.JoiningDate
        });
    }

    [HttpPut("{trainerId:guid}/assign-members")]
    [Authorize(Roles = "Admin")]
    public IActionResult AssignMembers(Guid trainerId, [FromBody] AssignMembersToTrainerRequest request)
    {
        var trainerExists = _context.Users.Any(u => u.Id == trainerId && u.Role == AppRole.Trainer);
        if (!trainerExists)
        {
            return NotFound(new { message = "Trainer not found." });
        }

        var members = _context.Members.Where(m => request.MemberIds.Contains(m.Id)).ToList();
        if (members.Count != request.MemberIds.Count)
        {
            return BadRequest(new { message = "One or more members were not found." });
        }

        foreach (var member in members)
        {
            member.AssignedTrainerId = trainerId;
        }

        _context.SaveChanges();
        return Ok(new { trainerId, assignedCount = members.Count });
    }

    [HttpGet("assigned-members")]
    [Authorize(Roles = "Trainer")]
    public IActionResult GetAssignedMembersForLoggedInTrainer()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var trainerId))
        {
            return Unauthorized(new { message = "Invalid trainer token." });
        }

        var members = _context.Members.Where(m => m.AssignedTrainerId == trainerId).ToList();
        return Ok(members);
    }

    [HttpGet("{trainerId:guid}/assigned-members")]
    [Authorize(Roles = "Admin,Trainer")]
    public IActionResult GetAssignedMembers(Guid trainerId)
    {
        if (User.IsInRole("Trainer"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdClaim, out var currentTrainerId) || currentTrainerId != trainerId)
            {
                return Forbid();
            }
        }

        var trainerExists = _context.Users.Any(u => u.Id == trainerId && u.Role == AppRole.Trainer);
        if (!trainerExists)
        {
            return NotFound(new { message = "Trainer not found." });
        }

        var members = _context.Members.Where(m => m.AssignedTrainerId == trainerId).ToList();
        return Ok(members);
    }

    [HttpPost("{trainerId:guid}/salaries")]
    [Authorize(Roles = "Admin")]
    public IActionResult AddSalary(Guid trainerId, [FromBody] CreateTrainerSalaryRequest request)
    {
        var trainerExists = _context.Users.Any(u => u.Id == trainerId && u.Role == AppRole.Trainer);
        if (!trainerExists)
        {
            return NotFound(new { message = "Trainer not found." });
        }

        var duplicate = _context.TrainerSalaries.Any(s =>
            s.TrainerUserId == trainerId && s.Year == request.Year && s.Month == request.Month);
        if (duplicate)
        {
            return Conflict(new { message = "Salary for this trainer and month already exists." });
        }

        var salary = new TrainerSalary
        {
            Id = Guid.NewGuid(),
            TrainerUserId = trainerId,
            Year = request.Year,
            Month = request.Month,
            Amount = request.Amount,
            IsPaid = request.IsPaid,
            PaidOn = request.PaidOn,
            Remarks = request.Remarks.Trim()
        };

        _context.TrainerSalaries.Add(salary);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetSalaryById), new { salaryId = salary.Id }, salary);
    }

    [HttpGet("{trainerId:guid}/salaries")]
    [Authorize(Roles = "Admin,Trainer")]
    public IActionResult GetSalaries(Guid trainerId)
    {
        if (User.IsInRole("Trainer"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userIdClaim, out var currentTrainerId) || currentTrainerId != trainerId)
            {
                return Forbid();
            }
        }

        var salaries = _context.TrainerSalaries
            .Where(s => s.TrainerUserId == trainerId)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ToList();
        return Ok(salaries);
    }

    [HttpGet("salaries/{salaryId:guid}")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetSalaryById(Guid salaryId)
    {
        var salary = _context.TrainerSalaries.FirstOrDefault(s => s.Id == salaryId);
        return salary is null ? NotFound(new { message = "Salary record not found." }) : Ok(salary);
    }

    [HttpPut("salaries/{salaryId:guid}")]
    [Authorize(Roles = "Admin")]
    public IActionResult UpdateSalary(Guid salaryId, [FromBody] UpdateTrainerSalaryRequest request)
    {
        var salary = _context.TrainerSalaries.FirstOrDefault(s => s.Id == salaryId);
        if (salary is null)
        {
            return NotFound(new { message = "Salary record not found." });
        }

        salary.Amount = request.Amount;
        salary.IsPaid = request.IsPaid;
        salary.PaidOn = request.PaidOn;
        salary.Remarks = request.Remarks.Trim();
        _context.SaveChanges();
        return Ok(salary);
    }
}
