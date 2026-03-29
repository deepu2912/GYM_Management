using GymManagement.Api.Data;
using GymManagement.Api.DTOs;
using GymManagement.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/subscription-plans")]
[Authorize]
public class SubscriptionPlansController : ControllerBase
{
    private readonly AppDbContext _context;

    public SubscriptionPlansController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult GetAll()
    {
        var items = _context.SubscriptionPlans
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList()
            .Select(Map)
            .ToList();

        return Ok(items);
    }

    [HttpGet("active")]
    public IActionResult GetActive()
    {
        var items = _context.SubscriptionPlans
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList()
            .Select(Map)
            .ToList();

        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult Create([FromBody] UpsertSubscriptionPlanRequest request)
    {
        if (!ValidateRequest(request, out var validationError))
        {
            return BadRequest(new { message = validationError });
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (_context.SubscriptionPlans.Any(x => x.Code == code))
        {
            return Conflict(new { message = "Subscription plan code already exists." });
        }

        var entity = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            Price = request.Price,
            DurationMonths = request.DurationMonths,
            IsLifetime = request.IsLifetime,
            IsMaintenance = request.IsMaintenance,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            Description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : request.Description.Trim(),
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow,
        };

        _context.SubscriptionPlans.Add(entity);
        _context.SaveChanges();

        return Ok(Map(entity));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult Update(Guid id, [FromBody] UpsertSubscriptionPlanRequest request)
    {
        var entity = _context.SubscriptionPlans.FirstOrDefault(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(new { message = "Subscription plan not found." });
        }

        if (!ValidateRequest(request, out var validationError))
        {
            return BadRequest(new { message = validationError });
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (_context.SubscriptionPlans.Any(x => x.Id != id && x.Code == code))
        {
            return Conflict(new { message = "Subscription plan code already exists." });
        }

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Price = request.Price;
        entity.DurationMonths = request.DurationMonths;
        entity.IsLifetime = request.IsLifetime;
        entity.IsMaintenance = request.IsMaintenance;
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : request.Description.Trim();
        entity.UpdatedOn = DateTime.UtcNow;

        _context.SaveChanges();
        return Ok(Map(entity));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult UpdateStatus(Guid id, [FromBody] UpdateSubscriptionPlanStatusRequest request)
    {
        var entity = _context.SubscriptionPlans.FirstOrDefault(x => x.Id == id);
        if (entity is null)
        {
            return NotFound(new { message = "Subscription plan not found." });
        }

        entity.IsActive = request.IsActive;
        entity.UpdatedOn = DateTime.UtcNow;
        _context.SaveChanges();

        return Ok(Map(entity));
    }

    private static SubscriptionPlanResponse Map(SubscriptionPlan entity)
    {
        return new SubscriptionPlanResponse
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Price = entity.Price,
            DurationMonths = entity.DurationMonths,
            IsLifetime = entity.IsLifetime,
            IsMaintenance = entity.IsMaintenance,
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder,
            Description = entity.Description
        };
    }

    private static bool ValidateRequest(UpsertSubscriptionPlanRequest request, out string? message)
    {
        if (request.IsLifetime && request.IsMaintenance)
        {
            message = "A plan cannot be both lifetime and maintenance.";
            return false;
        }

        if (request.IsMaintenance && request.DurationMonths <= 0)
        {
            message = "Maintenance plan duration must be greater than 0 months.";
            return false;
        }

        if (!request.IsLifetime && request.DurationMonths <= 0)
        {
            message = "Duration months must be greater than 0 for non-lifetime plans.";
            return false;
        }

        message = null;
        return true;
    }
}
