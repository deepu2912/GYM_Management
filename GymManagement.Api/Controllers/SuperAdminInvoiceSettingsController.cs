using GymManagement.Api.Data;
using GymManagement.Api.DTOs;
using GymManagement.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/super-admin/invoice-settings")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminInvoiceSettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SuperAdminInvoiceSettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var settings = _context.SuperAdminInvoiceSettings.FirstOrDefault();
        if (settings is null)
        {
            return Ok(new SuperAdminInvoiceSettingsResponse());
        }

        return Ok(ToResponse(settings));
    }

    [HttpPut]
    public IActionResult Upsert([FromBody] UpsertSuperAdminInvoiceSettingsRequest request)
    {
        var settings = _context.SuperAdminInvoiceSettings.FirstOrDefault();
        if (settings is null)
        {
            settings = new SuperAdminInvoiceSettings
            {
                Id = Guid.NewGuid(),
                CreatedOn = DateTime.UtcNow
            };
            _context.SuperAdminInvoiceSettings.Add(settings);
        }

        settings.BusinessName = request.BusinessName.Trim();
        settings.Email = request.Email.Trim();
        settings.Phone = request.Phone.Trim();
        settings.AddressLine = request.AddressLine.Trim();
        settings.City = request.City.Trim();
        settings.State = request.State.Trim();
        settings.Pincode = request.Pincode.Trim();
        settings.GstNumber = request.GstNumber.Trim();
        settings.BankName = request.BankName.Trim();
        settings.AccountName = request.AccountName.Trim();
        settings.AccountNumber = request.AccountNumber.Trim();
        settings.IfscCode = request.IfscCode.Trim();
        settings.UpiId = string.IsNullOrWhiteSpace(request.UpiId) ? null : request.UpiId.Trim();
        settings.AuthorizedSignatory = request.AuthorizedSignatory.Trim();
        settings.TermsAndConditions = request.TermsAndConditions.Trim();
        settings.UpdatedOn = DateTime.UtcNow;

        _context.SaveChanges();
        return Ok(ToResponse(settings));
    }

    private static SuperAdminInvoiceSettingsResponse ToResponse(SuperAdminInvoiceSettings settings)
    {
        return new SuperAdminInvoiceSettingsResponse
        {
            Id = settings.Id,
            BusinessName = settings.BusinessName,
            Email = settings.Email,
            Phone = settings.Phone,
            AddressLine = settings.AddressLine,
            City = settings.City,
            State = settings.State,
            Pincode = settings.Pincode,
            GstNumber = settings.GstNumber,
            BankName = settings.BankName,
            AccountName = settings.AccountName,
            AccountNumber = settings.AccountNumber,
            IfscCode = settings.IfscCode,
            UpiId = settings.UpiId,
            AuthorizedSignatory = settings.AuthorizedSignatory,
            TermsAndConditions = settings.TermsAndConditions,
            CreatedOn = settings.CreatedOn,
            UpdatedOn = settings.UpdatedOn
        };
    }
}
