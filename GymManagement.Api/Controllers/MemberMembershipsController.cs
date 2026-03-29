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
public class MemberMembershipsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MemberMembershipsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Trainer")]
    public IActionResult GetAll()
    {
        AutoDeactivateExpiredMemberships();
        var records = QueryMemberMembershipResponses()
            .OrderByDescending(x => x.CreatedOn)
            .ThenByDescending(x => x.StartDate)
            .ToList();
        return Ok(records);
    }

    [HttpGet("paged")]
    [Authorize(Roles = "Admin,Trainer")]
    public IActionResult GetPaged([FromQuery] GetMemberMembershipsPagedRequest request)
    {
        AutoDeactivateExpiredMemberships();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : Math.Min(request.PageSize, 100);

        var query = QueryMemberMembershipResponses();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var search = request.Name.Trim().ToLower();
            query = query.Where(x =>
                (x.Member != null && x.Member.Name.ToLower().Contains(search)) ||
                (x.MembershipPlan != null && x.MembershipPlan.PlanName.ToLower().Contains(search)));
        }

        if (request.PlanId.HasValue)
        {
            query = query.Where(x => x.MembershipPlanId == request.PlanId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Type) &&
            Enum.TryParse<MembershipType>(request.Type, true, out var parsedType))
        {
            query = query.Where(x =>
                _context.MembershipPlans.Any(mp => mp.Id == x.MembershipPlanId && mp.MembershipType == parsedType));
        }

        if (request.CreatedFrom.HasValue)
        {
            var from = request.CreatedFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.CreatedOn >= from);
        }

        if (request.CreatedTo.HasValue)
        {
            var toExclusive = request.CreatedTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.CreatedOn < toExclusive);
        }

        if (request.EndingInDays.HasValue)
        {
            var days = Math.Max(request.EndingInDays.Value, 0);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var till = today.AddDays(days);
            query = query.Where(x => x.EndDate >= today && x.EndDate <= till);
        }

        if (request.PendingCollectionOnly)
        {
            query = query.Where(x =>
                _context.MemberMemberships.Any(mm =>
                    mm.Id == x.Id &&
                    (mm.PlanPriceAtEnrollment - mm.Discount) >
                    (_context.Payments
                        .Where(p => p.MemberMembershipId == mm.Id)
                        .Sum(p => (decimal?)p.Amount) ?? 0m)));
        }

        if (request.CollectedThisMonthOnly)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonthStart = monthStart.AddMonths(1);

            query = query.Where(x =>
                _context.Payments.Any(p =>
                    p.MemberMembershipId == x.Id &&
                    p.PaidOn >= monthStart &&
                    p.PaidOn < nextMonthStart));
        }

        query = query.OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.StartDate);

        var totalCount = query.Count();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new MemberMembershipsPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = items
        });
    }

    [HttpGet("dashboard-summary")]
    [Authorize(Roles = "Admin,Trainer")]
    public IActionResult GetDashboardSummary([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedFrom = fromDate ?? new DateOnly(today.Year, today.Month, 1);
        var resolvedTo = toDate ?? new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        if (resolvedFrom > resolvedTo)
        {
            return BadRequest(new { message = "fromDate must be less than or equal to toDate." });
        }

        var createdFrom = resolvedFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var createdToExclusive = resolvedTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var filteredMemberships = _context.MemberMemberships
            .AsNoTracking()
            .Where(x => x.CreatedOn >= createdFrom && x.CreatedOn < createdToExclusive)
            .Select(x => new
            {
                MembershipType = x.MembershipPlan != null ? x.MembershipPlan.MembershipType : MembershipType.Single,
                HasSecondaryMember = x.SecondaryMemberId.HasValue
            });

        var coupleMembershipCount = filteredMemberships.Count(x => x.MembershipType == MembershipType.Couple);
        var coupleUsersCount = filteredMemberships.Sum(x =>
            x.MembershipType == MembershipType.Couple
                ? (x.HasSecondaryMember ? 2 : 1)
                : 0);
        var singleUsersCount = filteredMemberships.Sum(x =>
            x.MembershipType == MembershipType.Couple
                ? 0
                : 1);

        var response = new MemberMembershipDashboardSummaryResponse
        {
            FromDate = resolvedFrom,
            ToDate = resolvedTo,
            CoupleMembershipCount = coupleMembershipCount,
            CoupleUsersCount = coupleUsersCount,
            SingleUsersCount = singleUsersCount,
            TotalMembershipUsers = singleUsersCount + coupleUsersCount
        };

        return Ok(response);
    }

    [HttpGet("member/{memberId:guid}")]
    [Authorize(Roles = "Admin,Trainer,Member")]
    public IActionResult GetByMember(Guid memberId)
    {
        AutoDeactivateExpiredMemberships();
        var gymTenantId = GetCurrentGymTenantId();

        if (!User.IsInRole("Member") && !_context.Members.Any(x => x.Id == memberId && x.GymTenantId == gymTenantId))
        {
            return NotFound(new { message = "Member not found." });
        }

        if (User.IsInRole("Member"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var member = _context.Members.FirstOrDefault(x => x.Id == memberId && x.GymTenantId == gymTenantId);
            if (member is null)
            {
                return NotFound(new { message = "Member not found." });
            }

            if (!Guid.TryParse(userId, out var currentUserId) || member.UserId != currentUserId)
            {
                return Forbid();
            }
        }

        var records = QueryMemberMembershipResponses()
            .Where(x => x.MemberId == memberId || x.SecondaryMemberId == memberId)
            .OrderByDescending(x => x.CreatedOn)
            .ThenByDescending(x => x.StartDate)
            .ToList();
        return Ok(records);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Trainer")]
    public IActionResult GetById(Guid id)
    {
        AutoDeactivateExpiredMemberships();
        var record = QueryMemberMembershipResponses()
            .FirstOrDefault(x => x.Id == id);
        return record is null ? NotFound(new { message = "Membership record not found." }) : Ok(record);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult Create([FromBody] CreateMemberMembershipRequest request)
    {
        var gymTenantId = GetCurrentGymTenantId();
        if (!_context.Members.Any(x => x.Id == request.MemberId && x.GymTenantId == gymTenantId))
        {
            return NotFound(new { message = "Member not found." });
        }

        var plan = _context.MembershipPlans.FirstOrDefault(x => x.Id == request.MembershipPlanId && x.GymTenantId == gymTenantId);
        if (plan is null)
        {
            return NotFound(new { message = "Membership plan not found." });
        }
        if (!plan.IsActive)
        {
            return BadRequest(new { message = "Inactive plan cannot be used for new member links." });
        }

        var validationError = ValidateMembershipTypeAndSecondaryMember(
            request.MemberId,
            plan.MembershipType,
            request.SecondaryMemberId);
        if (validationError is not null)
        {
            return validationError;
        }

        if (request.Discount < 0 || request.Discount > plan.Price)
        {
            return BadRequest(new { message = "Discount must be between 0 and plan amount." });
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest(new { message = "EndDate must be greater than or equal to StartDate." });
        }

        var record = new MemberMembership
        {
            Id = Guid.NewGuid(),
            MemberId = request.MemberId,
            SecondaryMemberId = request.SecondaryMemberId,
            MembershipPlanId = request.MembershipPlanId,
            CreatedOn = DateTime.UtcNow,
            PlanPriceAtEnrollment = plan.Price,
            Discount = request.Discount,
            Description = request.Description.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _context.MemberMemberships.Add(record);
        _context.SaveChanges();
        var response = QueryMemberMembershipResponses().FirstOrDefault(x => x.Id == record.Id);
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Update(Guid id, [FromBody] UpdateMemberMembershipRequest request)
    {
        var gymTenantId = GetCurrentGymTenantId();
        var record = _context.MemberMemberships.FirstOrDefault(x => x.Id == id);
        if (record is null)
        {
            return NotFound(new { message = "Membership record not found." });
        }
        if (!_context.Members.Any(x => x.Id == record.MemberId && x.GymTenantId == gymTenantId))
        {
            return NotFound(new { message = "Membership record not found." });
        }

        if (!_context.Members.Any(x => x.Id == request.MemberId && x.GymTenantId == gymTenantId))
        {
            return NotFound(new { message = "Member not found." });
        }

        var plan = _context.MembershipPlans.FirstOrDefault(x => x.Id == request.MembershipPlanId && x.GymTenantId == gymTenantId);
        if (plan is null)
        {
            return NotFound(new { message = "Membership plan not found." });
        }
        var isPlanChanged = record.MembershipPlanId != request.MembershipPlanId;
        if (isPlanChanged && !plan.IsActive)
        {
            return BadRequest(new { message = "Inactive plan cannot be used for new member links." });
        }

        var validationError = ValidateMembershipTypeAndSecondaryMember(
            request.MemberId,
            plan.MembershipType,
            request.SecondaryMemberId);
        if (validationError is not null)
        {
            return validationError;
        }

        var hasPayments = _context.Payments.Any(p => p.MemberMembershipId == record.Id);
        if (isPlanChanged && hasPayments)
        {
            return BadRequest(new { message = "Membership plan cannot be changed once payment is collected." });
        }
        var effectivePlanPrice = isPlanChanged ? plan.Price : record.PlanPriceAtEnrollment;

        if (request.Discount < 0 || request.Discount > effectivePlanPrice)
        {
            return BadRequest(new { message = "Discount must be between 0 and plan amount." });
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest(new { message = "EndDate must be greater than or equal to StartDate." });
        }

        record.MemberId = request.MemberId;
        record.SecondaryMemberId = request.SecondaryMemberId;
        record.MembershipPlanId = request.MembershipPlanId;
        if (isPlanChanged && !hasPayments)
        {
            record.PlanPriceAtEnrollment = plan.Price;
        }
        record.Discount = request.Discount;
        record.Description = request.Description.Trim();
        record.StartDate = request.StartDate;
        record.EndDate = request.EndDate;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        record.IsActive = request.EndDate < today ? false : request.IsActive;
        _context.SaveChanges();
        var response = QueryMemberMembershipResponses().FirstOrDefault(x => x.Id == record.Id);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(Guid id)
    {
        var gymTenantId = GetCurrentGymTenantId();
        var record = _context.MemberMemberships.FirstOrDefault(x => x.Id == id);
        if (record is null)
        {
            return NotFound(new { message = "Membership record not found." });
        }
        if (!_context.Members.Any(x => x.Id == record.MemberId && x.GymTenantId == gymTenantId))
        {
            return NotFound(new { message = "Membership record not found." });
        }

        var hasPayments = _context.Payments.Any(p => p.MemberMembershipId == id);
        if (hasPayments)
        {
            return BadRequest(new
            {
                message = "Payment is collected on behalf of this plan so it can't be deleted."
            });
        }

        _context.MemberMemberships.Remove(record);
        _context.SaveChanges();
        return NoContent();
    }

    private IQueryable<MemberMembershipResponse> QueryMemberMembershipResponses()
    {
        var gymTenantId = GetCurrentGymTenantId();
        return _context.MemberMemberships
            .AsNoTracking()
            .Where(x =>
                x.Member != null &&
                x.Member.GymTenantId == gymTenantId &&
                x.MembershipPlan != null &&
                x.MembershipPlan.GymTenantId == gymTenantId)
            .Select(x => new MemberMembershipResponse
            {
                Id = x.Id,
                MemberId = x.MemberId,
                SecondaryMemberId = x.SecondaryMemberId,
                MembershipPlanId = x.MembershipPlanId,
                CreatedOn = x.CreatedOn,
                PlanPriceAtEnrollment = x.PlanPriceAtEnrollment,
                Discount = x.Discount,
                Description = x.Description,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsActive = x.IsActive,
                HasPayments = _context.Payments.Any(p => p.MemberMembershipId == x.Id),
                Member = x.Member == null
                    ? null
                    : new MemberSummaryDto
                    {
                        Id = x.Member.Id,
                        Name = x.Member.Name,
                        Email = x.Member.Email
                    },
                SecondaryMember = x.SecondaryMember == null
                    ? null
                    : new MemberSummaryDto
                    {
                        Id = x.SecondaryMember.Id,
                        Name = x.SecondaryMember.Name,
                        Email = x.SecondaryMember.Email
                    },
                MembershipPlan = x.MembershipPlan == null
                    ? null
                    : new MembershipPlanSummaryDto
                    {
                        Id = x.MembershipPlan.Id,
                        PlanName = x.MembershipPlan.PlanName,
                        MembershipType = x.MembershipPlan.MembershipType,
                        Duration = x.MembershipPlan.Duration,
                        Price = x.PlanPriceAtEnrollment
                    }
            });
    }

    private IActionResult? ValidateMembershipTypeAndSecondaryMember(
        Guid memberId,
        MembershipType membershipType,
        Guid? secondaryMemberId)
    {
        var gymTenantId = GetCurrentGymTenantId();
        if (membershipType == MembershipType.Single && secondaryMemberId.HasValue)
        {
            return BadRequest(new { message = "SecondaryMemberId is only allowed for Couple membership type." });
        }

        if (membershipType == MembershipType.Couple && !secondaryMemberId.HasValue)
        {
            return BadRequest(new { message = "SecondaryMemberId is required for Couple membership type." });
        }

        if (!secondaryMemberId.HasValue)
        {
            return null;
        }

        if (secondaryMemberId.Value == memberId)
        {
            return BadRequest(new { message = "Primary and secondary members must be different." });
        }

        if (!_context.Members.Any(x => x.Id == secondaryMemberId.Value && x.GymTenantId == gymTenantId))
        {
            return NotFound(new { message = "Secondary member not found." });
        }

        return null;
    }

    private void AutoDeactivateExpiredMemberships()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expiredActiveRecords = _context.MemberMemberships
            .Where(x => x.IsActive && x.EndDate < today)
            .ToList();

        if (expiredActiveRecords.Count == 0)
        {
            return;
        }

        foreach (var record in expiredActiveRecords)
        {
            record.IsActive = false;
        }

        _context.SaveChanges();
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
