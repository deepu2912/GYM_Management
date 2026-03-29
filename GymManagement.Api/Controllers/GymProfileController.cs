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
public class GymProfileController : ControllerBase
{
    private readonly AppDbContext _context;

    public GymProfileController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Trainer,Member")]
    public IActionResult Get()
    {
        var gymTenantId = GetCurrentGymTenantId();
        GymProfile? gymProfile;
        if (gymTenantId.HasValue)
        {
            gymProfile = _context.GymProfiles.FirstOrDefault(x => x.GymTenantId == gymTenantId.Value);
            if (gymProfile is null)
            {
                var tenant = _context.GymTenants.FirstOrDefault(x => x.Id == gymTenantId.Value);
                if (tenant is not null)
                {
                    gymProfile = new GymProfile
                    {
                        Id = Guid.NewGuid(),
                        GymTenantId = tenant.Id,
                        GymName = tenant.GymName,
                        Email = tenant.Email,
                        Phone = tenant.Phone,
                        AddressLine = tenant.AddressLine,
                        City = tenant.City,
                        State = tenant.State,
                        Pincode = tenant.Pincode,
                        GstNumber = tenant.GstNumber,
                        BankName = tenant.BankName,
                        AccountName = tenant.AccountName,
                        AccountNumber = tenant.AccountNumber,
                        IfscCode = tenant.IfscCode,
                        UpiId = tenant.UpiId,
                        HsnSacCode = tenant.HsnSacCode,
                        GstRatePercent = tenant.GstRatePercent,
                        IsGstApplicable = tenant.IsGstApplicable
                    };
                    _context.GymProfiles.Add(gymProfile);
                    _context.SaveChanges();
                }
            }
        }
        else
        {
            gymProfile = _context.GymProfiles.FirstOrDefault();
        }

        return gymProfile is null
            ? NotFound(new { message = "Gym profile not configured yet." })
            : Ok(gymProfile);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public IActionResult Upsert([FromBody] UpsertGymProfileRequest request)
    {
        var gymTenantId = GetCurrentGymTenantId();
        var profile = gymTenantId.HasValue
            ? _context.GymProfiles.FirstOrDefault(x => x.GymTenantId == gymTenantId.Value)
            : _context.GymProfiles.FirstOrDefault();

        if (profile is null)
        {
            profile = new GymProfile
            {
                Id = Guid.NewGuid(),
                GymTenantId = gymTenantId
            };
            _context.GymProfiles.Add(profile);
        }

        profile.GymName = request.GymName.Trim();
        profile.Email = request.Email.Trim();
        profile.Phone = request.Phone.Trim();
        profile.AddressLine = request.AddressLine.Trim();
        profile.City = request.City.Trim();
        profile.State = request.State.Trim();
        profile.Pincode = request.Pincode.Trim();
        if (request.IsGstApplicable && string.IsNullOrWhiteSpace(request.GstNumber))
        {
            return BadRequest(new { message = "GST Number is required when GST Applicable is enabled." });
        }

        profile.GstNumber = request.IsGstApplicable ? request.GstNumber!.Trim() : string.Empty;
        profile.BankName = request.BankName.Trim();
        profile.AccountName = request.AccountName.Trim();
        profile.AccountNumber = request.AccountNumber.Trim();
        profile.IfscCode = request.IfscCode.Trim();
        profile.UpiId = request.UpiId?.Trim();
        profile.LogoDataUri = string.IsNullOrWhiteSpace(request.LogoDataUri) ? null : request.LogoDataUri.Trim();
        profile.HsnSacCode = string.IsNullOrWhiteSpace(request.HsnSacCode) ? "9997" : request.HsnSacCode.Trim();
        profile.GstRatePercent = Math.Clamp(request.GstRatePercent, 0, 100);
        profile.IsGstApplicable = request.IsGstApplicable;

        _context.SaveChanges();
        return Ok(profile);
    }

    private Guid? GetCurrentGymTenantId()
    {
        var claim = User.FindFirst("gymTenantId")?.Value;
        if (Guid.TryParse(claim, out var gymTenantIdFromClaim))
        {
            return gymTenantIdFromClaim;
        }

        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return null;
        }

        return _context.Users.FirstOrDefault(x => x.Id == userId)?.GymTenantId;
    }
}
