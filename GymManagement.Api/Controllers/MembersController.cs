using System.Security.Claims;
using GymManagement.Api.Data;
using GymManagement.Api.DTOs;
using GymManagement.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly AppDbContext _context;

    public MembersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Trainer")]
    public IActionResult GetAll()
    {
        var gymTenantId = GetCurrentGymTenantId();
        return Ok(_context.Members.Where(x => x.GymTenantId == gymTenantId).ToList());
    }

    [HttpGet("paged")]
    [Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> GetPaged([FromQuery] GetMembersPagedRequest request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : Math.Min(request.PageSize, 100);

        var gymTenantId = GetCurrentGymTenantId();
        IQueryable<Member> query = _context.Members
            .AsNoTracking()
            .Where(x => x.GymTenantId == gymTenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Name, term) ||
                EF.Functions.Like(x.Email, term) ||
                EF.Functions.Like(x.Phone, term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var items = await query
            .OrderByDescending(x => x.JoiningDate)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new MembersPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Trainer,Member")]
    public IActionResult GetById(Guid id)
    {
        var member = _context.Members.FirstOrDefault(m => m.Id == id);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        var gymTenantId = GetCurrentGymTenantId();
        if ((User.IsInRole("Admin") || User.IsInRole("Trainer")) && member.GymTenantId != gymTenantId)
        {
            return NotFound(new { message = "Member not found." });
        }

        if (User.IsInRole("Member"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId) || member.UserId != currentUserId)
            {
                return Forbid();
            }
        }

        return Ok(member);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult Create([FromBody] CreateMemberRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToUpper();
        if (_context.Members.Any(m => m.Email.ToUpper() == normalizedEmail))
        {
            return Conflict(new { message = "A member with this email already exists." });
        }

        var member = new Member
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Empty,
            GymTenantId = GetCurrentGymTenantId(),
            Name = request.Name,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Phone = request.Phone,
            Email = request.Email,
            AddressLine = request.AddressLine,
            City = request.City,
            State = request.State,
            Pincode = request.Pincode,
            Height = request.Height,
            Weight = request.Weight,
            JoiningDate = request.JoiningDate,
            MembershipStatus = MembershipStatus.Active
        };

        _context.Members.Add(member);
        _context.SaveChanges();
        return CreatedAtAction(nameof(GetById), new { id = member.Id }, member);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Update(Guid id, [FromBody] UpdateMemberRequest request)
    {
        var member = _context.Members.FirstOrDefault(m => m.Id == id);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }
        if (member.GymTenantId != GetCurrentGymTenantId())
        {
            return NotFound(new { message = "Member not found." });
        }

        member.Name = request.Name;
        member.DateOfBirth = request.DateOfBirth;
        member.Gender = request.Gender;
        member.Phone = request.Phone;
        member.Email = request.Email;
        member.AddressLine = request.AddressLine;
        member.City = request.City;
        member.State = request.State;
        member.Pincode = request.Pincode;
        member.Height = request.Height;
        member.Weight = request.Weight;
        member.JoiningDate = request.JoiningDate;
        member.MembershipStatus = request.MembershipStatus;

        _context.SaveChanges();
        return Ok(member);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(Guid id)
    {
        var member = _context.Members.FirstOrDefault(m => m.Id == id);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }
        if (member.GymTenantId != GetCurrentGymTenantId())
        {
            return NotFound(new { message = "Member not found." });
        }

        _context.Members.Remove(member);
        _context.SaveChanges();
        return NoContent();
    }

    [HttpPut("{id:guid}/assign-trainer")]
    [Authorize(Roles = "Admin")]
    public IActionResult AssignTrainer(Guid id, [FromBody] AssignTrainerRequest request)
    {
        var member = _context.Members.FirstOrDefault(m => m.Id == id);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }
        if (member.GymTenantId != GetCurrentGymTenantId())
        {
            return NotFound(new { message = "Member not found." });
        }

        var trainer = _context.Users.FirstOrDefault(u => u.Id == request.TrainerId && u.Role == AppRole.Trainer);
        if (trainer is null)
        {
            return NotFound(new { message = "Trainer not found." });
        }

        member.AssignedTrainerId = trainer.Id;
        _context.SaveChanges();
        return Ok(member);
    }

    [HttpPost("{id:guid}/upload-photo")]
    [HttpPut("{id:guid}/upload-photo")]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> UploadProfilePhoto(Guid id, IFormFile? file)
    {
        var member = _context.Members.FirstOrDefault(m => m.Id == id);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }
        if ((User.IsInRole("Admin") || User.IsInRole("Trainer")) && member.GymTenantId != GetCurrentGymTenantId())
        {
            return NotFound(new { message = "Member not found." });
        }

        if (User.IsInRole("Member"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId) || member.UserId != currentUserId)
            {
                return Forbid();
            }
        }

        if (file is null)
        {
            return BadRequest(new { message = "Photo file is required." });
        }

        if (file.Length == 0)
        {
            return BadRequest(new { message = "File is empty." });
        }

        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{id}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        member.ProfilePhotoPath = $"/uploads/{fileName}";
        _context.SaveChanges();
        return Ok(new { member.Id, member.ProfilePhotoPath });
    }

    private Guid? GetCurrentGymTenantId()
    {
        var claim = User.FindFirst("gymTenantId")?.Value;
        if (Guid.TryParse(claim, out var gymTenantId))
        {
            return gymTenantId;
        }

        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return null;
        }

        return _context.Users.FirstOrDefault(x => x.Id == userId)?.GymTenantId;
    }
}
