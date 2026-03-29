using GymManagement.Api.Data;
using GymManagement.Api.DTOs;
using GymManagement.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembershipPlansController : ControllerBase
{
    private readonly AppDbContext _context;

    public MembershipPlansController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var plans = QueryMembershipPlans()
            .OrderBy(x => x.PlanName)
            .ToList();
        return Ok(plans);
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var plan = QueryMembershipPlans()
            .FirstOrDefault(x => x.Id == id);
        return plan is null ? NotFound(new { message = "Membership plan not found." }) : Ok(plan);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult Create([FromBody] CreateMembershipPlanRequest request)
    {
        var gymTenantId = GetCurrentGymTenantId();
        var exists = _context.MembershipPlans.Any(x =>
            x.GymTenantId == gymTenantId &&
            x.PlanName.ToUpper() == request.PlanName.Trim().ToUpper());
        if (exists)
        {
            return Conflict(new { message = "A membership plan with this name already exists." });
        }

        var plan = new MembershipPlan
        {
            Id = Guid.NewGuid(),
            GymTenantId = gymTenantId,
            PlanName = request.PlanName.Trim(),
            MembershipType = request.MembershipType,
            Duration = request.Duration,
            Price = request.Price,
            Description = request.Description.Trim(),
            IsActive = true
        };

        _context.MembershipPlans.Add(plan);
        _context.SaveChanges();
        var response = QueryMembershipPlans().First(x => x.Id == plan.Id);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Update(Guid id, [FromBody] UpdateMembershipPlanRequest request)
    {
        var plan = _context.MembershipPlans.FirstOrDefault(x => x.Id == id);
        if (plan is null)
        {
            return NotFound(new { message = "Membership plan not found." });
        }
        if (plan.GymTenantId != GetCurrentGymTenantId())
        {
            return NotFound(new { message = "Membership plan not found." });
        }

        var gymTenantId = GetCurrentGymTenantId();
        var duplicateName = _context.MembershipPlans.Any(x =>
            x.GymTenantId == gymTenantId &&
            x.Id != id &&
            x.PlanName.ToUpper() == request.PlanName.Trim().ToUpper());
        if (duplicateName)
        {
            return Conflict(new { message = "A membership plan with this name already exists." });
        }

        plan.PlanName = request.PlanName.Trim();
        plan.MembershipType = request.MembershipType;
        plan.Duration = request.Duration;
        plan.Price = request.Price;
        plan.Description = request.Description.Trim();
        _context.SaveChanges();
        var response = QueryMembershipPlans().First(x => x.Id == plan.Id);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(Guid id)
    {
        var plan = _context.MembershipPlans
            .Include(x => x.MemberMemberships)
            .FirstOrDefault(x => x.Id == id);

        if (plan is null)
        {
            return NotFound(new { message = "Membership plan not found." });
        }
        if (plan.GymTenantId != GetCurrentGymTenantId())
        {
            return NotFound(new { message = "Membership plan not found." });
        }

        if (plan.MemberMemberships.Count > 0)
        {
            return BadRequest(new { message = "Cannot delete plan linked to member memberships." });
        }

        _context.MembershipPlans.Remove(plan);
        _context.SaveChanges();
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public IActionResult UpdateStatus(Guid id, [FromBody] UpdateMembershipPlanStatusRequest request)
    {
        var plan = _context.MembershipPlans.FirstOrDefault(x => x.Id == id);
        if (plan is null)
        {
            return NotFound(new { message = "Membership plan not found." });
        }
        if (plan.GymTenantId != GetCurrentGymTenantId())
        {
            return NotFound(new { message = "Membership plan not found." });
        }

        plan.IsActive = request.IsActive;
        _context.SaveChanges();

        var response = QueryMembershipPlans().First(x => x.Id == id);
        return Ok(response);
    }

    private IQueryable<MembershipPlanResponse> QueryMembershipPlans()
    {
        var gymTenantId = GetCurrentGymTenantId();
        return _context.MembershipPlans
            .AsNoTracking()
            .Where(x => x.GymTenantId == gymTenantId)
            .Select(x => new MembershipPlanResponse
            {
                Id = x.Id,
                PlanName = x.PlanName,
                MembershipType = x.MembershipType,
                Duration = x.Duration,
                Price = x.Price,
                Description = x.Description,
                IsActive = x.IsActive,
                HasLinkedMemberships = _context.MemberMemberships.Any(mm => mm.MembershipPlanId == x.Id)
            });
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
